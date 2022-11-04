from pathlib import Path
import os

BOT_TOKEN = os.environ['BotTOKEN']
BASE_URL = ''

API_URL = 'http://127.0.0.1:9999'

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
