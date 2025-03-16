from ibm_watson import SpeechToTextV1
from ibm_cloud_sdk_core.authenticators import IAMAuthenticator
import os

print(f'API Key: {"Not set" if not os.environ.get("IBM_STT_API_KEY") else os.environ.get("IBM_STT_API_KEY")[:5] + "..."}')
print(f'Service URL: {os.environ.get("IBM_STT_SERVICE_URL", "Not set")}')
print('Trying to initialize...')

try:
    auth = IAMAuthenticator(os.environ.get('IBM_STT_API_KEY'))
    stt = SpeechToTextV1(authenticator=auth)
    stt.set_service_url(os.environ.get('IBM_STT_SERVICE_URL'))
    models = stt.list_models().get_result()
    print(f'Success! Found {len(models["models"])} models.')
except Exception as e:
    print(f'Error: {type(e).__name__}: {str(e)}') 