from aiogram import types
from keyboards.inline import Emoji

async def cmd_start(message: types.Message):
    await message.reply("Ну привет, будем смотреть мемасы!", reply_markup=Emoji.emojiRating())