import os
from pathlib import Path

import pandas as pd
import torch
from PIL import Image
from torch.nn.utils.rnn import pad_sequence
from torch.utils.data import Dataset

from .tokenizers import WordPunctTokenizer
from .vocab import SPECIAL_TOKENS


class MemeDataset(Dataset):
    def __init__(self, root, vocab, clip_preprocess, tokenizer=WordPunctTokenizer(),
                 split='train', num_classes=300, image_transform=None,
                 preload_images=True):
        assert split in ('train', 'val', 'test'), 'Incorrect data split'

        self.root = root
        self.split = split
        self.clip_preprocess = clip_preprocess
        self.tokenizer = tokenizer
        self.vocab = vocab
        self.image_transform = image_transform
        self.preload_images = preload_images

        self.num_classes = num_classes
        self._load_dataset()

    def _load_dataset(self):
        # load templates information
        fn_temp = os.path.join(self.root, 'templates.txt')
        assert os.path.exists(fn_temp), \
            f'Templates file {fn_temp} is not found'

        dir_imgs = os.path.join(self.root, 'images')
        assert os.path.isdir(dir_imgs), \
            f'Images directory {dir_imgs} is not found'

        self.templates = {}
        self.images = {}
        with open(fn_temp, 'r') as f:
            for line in f:
                label, _, url = line.strip().split('\t')
                filename = url.split('/')[-1]
                self.templates[label] = os.path.join(dir_imgs, filename)

                # preload images and apply transforms
                if self.preload_images:
                    img = self.clip_preprocess(Image.open(self.templates[label])).to(torch.float32)
                    # if self.image_transform is not None:
                    #     img = self.image_transform(img)
                    self.images[label] = img
                else:
                    self.images[label] = self.templates[label]

                if len(self.templates) == self.num_classes:
                    break

        # load captions
        fn_capt = os.path.join(self.root, f'captions_{self.split}.txt')
        assert os.path.exists(fn_capt), \
            f'Captions file {fn_capt} is not found'

        self.captions = []
        with open(fn_capt, 'r') as f:
            for i, line in enumerate(f):
                label, _, caption = line.strip().split('\t')
                if label in self.templates:
                    self.captions.append((label, caption))

    def _preprocess_text(self, text):
        # tokenize
        tokens = self.tokenizer.tokenize(text.lower())

        # replace with `UNK`
        tokens = [tok if tok in self.vocab.stoi else SPECIAL_TOKENS['UNK'] for tok in tokens]

        # add `EOS`
        tokens += [SPECIAL_TOKENS['EOS']]

        # convert to ids
        tokens = [self.vocab.stoi[tok] for tok in tokens]

        return tokens

    def __getitem__(self, idx):
        label, caption = self.captions[idx]
        img = self.images[label]

        # label and caption tokens
        label = torch.tensor(self._preprocess_text(label)).long()
        caption = torch.tensor(self._preprocess_text(caption)).long()

        # image transform
        if not self.preload_images:
            img = self.clip_preprocess(Image.open(img))
            # if self.image_transform is not None:
            #     img = self.image_transform(img)

        return label, caption, img

    def __len__(self):
        return len(self.captions)


class HarmfullMemeDataset(Dataset):
    def __init__(self, root, vocab, clip_preprocess, tokenizer=WordPunctTokenizer(),
                 split='train', ds_location='dataset/hatefull'):
        assert split in ('train', 'val', 'test'), 'Incorrect data split'

        self.root = root
        self.split = split
        self.clip_preprocess = clip_preprocess
        self.tokenizer = tokenizer
        self.vocab = vocab
        self.ds_location = ds_location

        self._load_dataset()

    def _load_dataset(self):
        scores_df = {
            'img':[],
            'score':[]
        }
        for txt in Path('dataset/hatefull/txt files').glob('*'):
            with open(str(txt)) as f:
                scores = f.read().split()
                avg = sum(map(int, scores))/len(scores)
                if avg >= 0:
                    scores_df['img'].append(f'img/{txt.stem}.png')
                    scores_df['score'].append(avg)


        scores_df = pd.DataFrame(scores_df)

        captions = pd.read_json(path_or_buf=f'{self.ds_location}/train.jsonl', lines=True)
        captions_val = pd.read_json(path_or_buf=f'{self.ds_location}/test_seen.jsonl', lines=True)
        captions_val_un = pd.read_json(path_or_buf=f'{self.ds_location}/test_unseen.jsonl', lines=True)
        captions_test = pd.read_json(path_or_buf=f'{self.ds_location}/dev_seen.jsonl', lines=True)
        captions_test_un = pd.read_json(path_or_buf=f'{self.ds_location}/dev_unseen.jsonl', lines=True)

        captions = pd.concat([captions, captions_test, captions_test_un, captions_val, captions_val_un], ignore_index=True)[['img','text']].drop_duplicates(['img'])

        captions = captions.merge(scores_df)
        self.dataset = captions

    def _preprocess_text(self, text):
        # tokenize
        tokens = self.tokenizer.tokenize(text.lower())

        # replace with `UNK`
        tokens = [tok if tok in self.vocab.stoi else SPECIAL_TOKENS['UNK'] for tok in tokens]

        # add `EOS`
        tokens += [SPECIAL_TOKENS['EOS']]

        # add <sep> to the middle
        tokens.insert(len(tokens)//2, SPECIAL_TOKENS['SEP'])

        # convert to ids
        tokens = [self.vocab.stoi[tok] for tok in tokens]

        return tokens

    def __getitem__(self, idx):
        img, caption = self.dataset.iloc[idx][['img', 'text']]

        # label and caption tokens
        caption = torch.tensor(self._preprocess_text(caption)).long()

        # image transform
        img = self.clip_preprocess(Image.open(f'{self.ds_location}/{img}'))

        return '', caption, img

    def __len__(self):
        return len(self.dataset)


def pad_collate(batch):
    """Batch collate with padding for Dataloader."""
    # unpack batch
    labels, captions, images = zip(*batch)

    # pad sequences
    labels = pad_sequence(labels, batch_first=True, padding_value=0)
    captions = pad_sequence(captions, batch_first=True, padding_value=0)
    images = torch.stack(images, dim=0)

    return labels, captions, images
