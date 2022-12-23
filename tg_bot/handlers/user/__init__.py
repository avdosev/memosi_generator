from aiogram import Dispatcher
from aiogram.dispatcher.filters import CommandStart, CommandHelp

from .commands import *

def setup(dp: Dispatcher):
    dp.register_message_handler(cmd_start, CommandStart())
    dp.register_message_handler(bot_help, CommandHelp())
    dp.register_message_handler(cmd_next, commands=['next'])
    dp.register_message_handler(image_sended, content_types=["photo"])
