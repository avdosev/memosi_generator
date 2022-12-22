FROM nvidia/cuda:11.7.1-cudnn8-runtime-ubuntu20.04
FROM python:3.10

WORKDIR /app

RUN apt update \
    && apt install gcc ffmpeg libsm6 libxext6 -y \
    && apt clean

RUN pip install "fastapi[all]"

COPY ./requirements.txt /app/requirements.txt

RUN pip install -r /app/requirements.txt \
    && rm -rf /root/.cache/pip

COPY ./app /app/