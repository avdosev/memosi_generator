import torch
from torch import nn


class ImageEncoderClip(nn.Module):
    def __init__(self, clip, emb_dim=256, dropout=0.2, spatial_features=False):
        super().__init__()

        self.clip = clip
        self.spatial_features = spatial_features

        # embedding layer
        self.linear = nn.Linear(512, emb_dim)
        self.bn = nn.BatchNorm1d(emb_dim)
        self.dropout = nn.Dropout(dropout)

    def forward(self, images):
        x = self.clip.encode_image(images).to(torch.float32)
        emb = self.dropout(self.bn(self.linear(x)))

        return emb
