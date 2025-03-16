from ibm_watson import TextToSpeechV1
from ibm_cloud_sdk_core.authenticators import IAMAuthenticator
import os

print(f'API Key: {"Not set" if not os.environ.get("IBM_TTS_API_KEY") else os.environ.get("IBM_TTS_API_KEY")[:5] + "..."}')
print(f'Service URL: {os.environ.get("IBM_TTS_SERVICE_URL", "Not set")}')
print('Trying to initialize...')

try:
    auth = IAMAuthenticator(os.environ.get('IBM_TTS_API_KEY'))
    tts = TextToSpeechV1(authenticator=auth)
    tts.set_service_url(os.environ.get('IBM_TTS_SERVICE_URL'))
    voices = tts.list_voices().get_result()
    print(f'Success! Found {len(voices["voices"])} voices.')
except Exception as e:
    print(f'Error: {type(e).__name__}: {str(e)}')
