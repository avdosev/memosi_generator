import io
from pathlib import Path

import clip
import torch
from fastapi import FastAPI, File, Response, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from PIL import Image

from data.vocab import Vocab
from models.final_model import CaptioningLSTM, CaptioningTransformerBase
from utils.meme_generator import get_a_meme

device = "cuda" if torch.cuda.is_available() else "cpu"
clip_model, clip_preprocess = clip.load("/model/ViT-B-32.pt", device=device)
for param in clip_model.parameters():
    param.requires_grad = False

vocab = Vocab.load('/model/vocab.txt')

lstm = CaptioningLSTM.from_pretrained('/model/lstm.pth', clip_model).to(device)





app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins = ['*'],
    allow_credentials = True,
    allow_methods=['*'],
    allow_headers=['*']
)



@app.post('/get_meme')
def get_meme(img: UploadFile = File(...)):
    with torch.no_grad():
        img_pil = Image.open(io.BytesIO(img.file.read())).convert('RGBA')
        img_t = clip_preprocess(img_pil).unsqueeze(0).to(device)
        meme = get_a_meme(lstm, img_t, img_pil, vocab)

    with io.BytesIO() as img_bytes:
        meme.save(img_bytes, format='PNG')
        return Response(content=img_bytes.getvalue(), media_type='image/png')
