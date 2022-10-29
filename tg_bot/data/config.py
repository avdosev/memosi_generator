from pathlib import Path
import os

BOT_TOKEN = os.environ['BotTOKEN']
BASE_URL = ''

LOGS_BASE_PATH = str(Path(__file__).parent.parent / 'logs')

admins = []

ip = {
    'db':    '',
    'redis': '',
}

mysql_info = {
    'host':     ip['db'],
    'user':     '',
    'password': '',
    'db':       '',
    'maxsize':  5,
    'port':     3306,
}

redis = {
    'host':     ip['redis'],
    'password': ''
}

emoji = {
    '💩': -2,
    '😡': -1,
    '😐': 0,
    '🤣': 1,
    '🤡': 2,
}
