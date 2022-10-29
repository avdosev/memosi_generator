from aiogram import Bot, Dispatcher
from aiogram.types import ParseMode
from aiogram.utils import executor
from aiogram.contrib.fsm_storage.memory import MemoryStorage

from data import config

async def on_startup(dp: Dispatcher):
    import filters
    import handlers
    filters.setup(dp)
    handlers.errors.setup(dp)
    handlers.user.setup(dp)

if __name__ == '__main__':
    bot = Bot(config.BOT_TOKEN, parse_mode=ParseMode.HTML, validate_token=True)
    storage = MemoryStorage()
    dp = Dispatcher(bot, storage=storage)

    executor.start_polling(dp, on_startup=on_startup, skip_updates=True)
