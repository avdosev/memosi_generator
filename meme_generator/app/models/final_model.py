"""Image captioning models."""
import torch
from torch import nn

from .transformer import SelfAttentionTransformerDecoder
from .image_encode import ImageEncoderClip
from .lstm import LSTMDecoder


class CaptioningLSTM(nn.Module):
    def __init__(self, num_tokens, clip, emb_dim=256, hidden_size=512, num_layers=2,
                 enc_dropout=0.3, dec_dropout=0.1):
        super(CaptioningLSTM, self).__init__()

        self.encoder = ImageEncoderClip(
            emb_dim=emb_dim,
            dropout=enc_dropout,
            clip=clip
        )

        self.decoder = LSTMDecoder(
            num_tokens=num_tokens,
            emb_dim=emb_dim,
            hidden_size=hidden_size,
            num_layers=num_layers,
            dropout=dec_dropout,
        )

        # hyperparameters dictionary
        self._hp = {
            'num_tokens': num_tokens,
            'emb_dim': emb_dim,
            'hidden_size': hidden_size,
            'num_layers': num_layers,
            'enc_dropout': enc_dropout,
            'dec_dropout': dec_dropout,
        }

    def forward(self, images, captions, lengths=None):
        emb = self.encoder(images)
        out = self.decoder(emb, captions, lengths)

        return out

    def generate(self, image, caption=None, max_len=25,
                 temperature=1.0, beam_size=10, top_k=50, eos_index=3):
        # get image embedding
        image_emb = self.encoder(image).unsqueeze(1)

        sampled_ids = self.decoder.generate(
            image_emb, caption=caption,
            max_len=max_len, temperature=temperature,
            beam_size=beam_size, top_k=top_k, eos_index=eos_index
        )

        return sampled_ids

    def save(self, ckpt_path):
        torch.save(
            {'model': self.state_dict(), 'hp': self._hp},
            ckpt_path
        )

    @staticmethod
    def from_pretrained(ckpt_path, clip):
        ckpt = torch.load(ckpt_path, map_location='cpu')
        hp = ckpt['hp']

        model = CaptioningLSTM(
            num_tokens=hp['num_tokens'],
            emb_dim=hp['emb_dim'],
            clip=clip,
            hidden_size=hp['hidden_size'],
            num_layers=hp['num_layers'],
            enc_dropout=hp['enc_dropout'],
            dec_dropout=hp['dec_dropout'],
        )
        model.load_state_dict(ckpt['model'])
        return model


class CaptioningTransformerBase(nn.Module):
    def __init__(self, num_tokens, clip, hid_dim=512, n_layers=6, n_heads=8, pf_dim=2048,
                 enc_dropout=0.3, dec_dropout=0.1, pad_index=0, max_len=128):
        super().__init__()

        self.encoder = ImageEncoderClip(
            emb_dim=hid_dim,
            dropout=enc_dropout,
            spatial_features=False,
            clip=clip
        )

        self.decoder = SelfAttentionTransformerDecoder(
            num_tokens=num_tokens,
            hid_dim=hid_dim,
            n_layers=n_layers,
            n_heads=n_heads,
            pf_dim=pf_dim,
            dropout=dec_dropout,
            pad_index=pad_index,
            max_len=max_len
        )

        # hyperparameters dictionary
        self._hp = {
            'num_tokens': num_tokens,
            'hid_dim': hid_dim,
            'n_layers': n_layers,
            'n_heads': n_heads,
            'pf_dim': pf_dim,
            'enc_dropout': enc_dropout,
            'dec_dropout': dec_dropout,
            'pad_index': pad_index,
            'max_len': max_len
        }

    def forward(self, images, captions, lengths=None):

        image_emb = self.encoder(images)
        out = self.decoder(captions, start_emb=image_emb)

        return out

    def generate(self, image, caption=None, max_len=25,
                 temperature=1.0, beam_size=10, top_k=50, eos_index=3):
        # get image embeddings
        image_emb = self.encoder(image)

        sampled_ids = self.decoder.generate(
            image_emb, caption=caption,
            max_len=max_len, temperature=temperature,
            beam_size=beam_size, top_k=top_k, eos_index=eos_index
        )

        return sampled_ids

    def save(self, ckpt_path):
        torch.save(
            {'model': self.state_dict(), 'hp': self._hp},
            ckpt_path
        )

    @staticmethod
    def from_pretrained(ckpt_path, clip):
        ckpt = torch.load(ckpt_path, map_location='cpu')
        hp = ckpt['hp']

        model = CaptioningTransformerBase(
            num_tokens=hp['num_tokens'],
            hid_dim=hp['hid_dim'],
            clip=clip,
            n_layers=hp['n_layers'],
            n_heads=hp['n_heads'],
            pf_dim=hp['pf_dim'],
            enc_dropout=hp['enc_dropout'],
            dec_dropout=hp['dec_dropout'],
            pad_index=hp['pad_index'],
            max_len=hp['max_len']
        )
        model.load_state_dict(ckpt['model'])
        return model
