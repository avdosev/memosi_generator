from aiogram import types
from keyboards.inline import Emoji
from api import *
import io

async def bot_help(msg: types.Message):
    text = [
        'Список команд: ',
        '/start - Начать диалог',
        '/help - Получить справку'
        '/next - Получить картинку для оценки'
    ]
    await msg.answer('\n'.join(text))

async def cmd_start(message: types.Message):
    lines = [
        'Ну привет, будем смотреть мемасы!'
        'Отправь /next и мы отправим картинку.'
        'Дальше просто оценивай.'
    ]
    await message.reply(' \n'.join(lines))

async def cmd_next(message: types.Message):
    resp = await api.get_next(user_id_from(message))
    print(resp)
    await message.answer_photo(await api.get_image(resp['url']), reply_markup=Emoji.emojiRating(imageId=resp['imageId']))


async def image_sended(message: types.Message):
    with io.BytesIO() as f:
        await message.photo[-1].download(destination=f)
        resp = await api.upload_image(f)
    print(resp)
    await message.answer_photo(await api.get_image(resp['url']), reply_markup=Emoji.emojiRating(imageId=resp['imageId']))

def user_id_from(message: types.Message):
    return str(message.from_user.id)
