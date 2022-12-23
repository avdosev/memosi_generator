import aiohttp
from data.config import API_URL


def _api(s):
    return f'{API_URL}/{s}'


async def estimate(imageId, clientId, estimate):
    async with aiohttp.ClientSession() as session:
        async with session.post(_api(f'api/images/estimate/{imageId}'), json={
            "estimate": estimate,
            "clientId": clientId,
        }) as resp:
            print(resp.status)


async def get_next(clientId, previousId=None):
    async with aiohttp.ClientSession() as session:
        async with session.get(_api('api/images/next'), params={'clientId': clientId}) as resp:
            return await resp.json()


async def get_image(path):
    async with aiohttp.ClientSession() as session:
        async with session.get(path) as resp:
            return await resp.read()

async def upload_image(pht):
    data = aiohttp.FormData()
    data.add_field('imageFile', pht)
    async with aiohttp.ClientSession() as session:
        async with session.post(_api('api/images/upload'), data=data) as resp:
            return await resp.json()
