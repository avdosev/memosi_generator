from .consts import InlineConstructor
from data import config

class Emoji(InlineConstructor):
    @staticmethod
    def emojiRating():
        schema = [len(config.emoji)]
        actions = [
            {'text': emoji[0], 'callback_data': emoji[1]} for emoji in sorted(config.emoji.items(), key=lambda x: x[1])
        ]
        return Emoji._create_kb(actions, schema)