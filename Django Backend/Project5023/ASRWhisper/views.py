from django.shortcuts import render
from django.http import JsonResponse, HttpResponse
from django.views.decorators.csrf import csrf_exempt
from TTS.api import TTS
import numpy as np
import base64
import openai
import json
import os
import wave
import datetime

# Create your views here.

outputPath = os.path.abspath("/home/zsx/OPaudios")
inputPath = os.path.abspath("/home/zsx/IPaudios/input.wav")

# I want you to be an interviewer for a human resources position. I will be a candidate and you will ask me interview questions for HR positions. I want you to answer only as an interviewer. Don't write all the questions at once. I want you to only interview me. Ask me questions, wait for my answer, and give me some comments. Don't write explanations. Ask me one by one like an interviewer, and wait for my answer. My first words are 'Hello interviewer'

messages = [
    {"role": "system", "content": "You are an interviewer for a human resource position."},
]
openai.api_key = os.getenv("OPENAI_API_KEY")
# print(outputPath)
# print(inputPath)

@csrf_exempt
def ASR(request):
    if (request.method == "POST"):

        audioData = request.body
        
        if not audioData:
            return JsonResponse({"error": "No audio file provided."})
        try:
            transcript = "the text for developing"
            sampleWidth = 2
            channels = 1
            sampleRate = 44100
            with wave.open(inputPath, 'wb') as wavFile:
                wavFile.setparams((channels, sampleWidth, sampleRate, 0, "NONE", "not compressed"))
                audioArray = np.frombuffer(audioData, dtype=np.int16)
                wavFile.writeframesraw(audioArray.tobytes())
            audioFile = open(inputPath, "rb")
            transcript = openai.Audio.transcribe("whisper-1", audioFile)['text']
            print(transcript)
            return JsonResponse({"transcript": transcript})
        except openai.InvalidRequestError as e:
            return JsonResponse({"error": "Error during transcribing: {}.".format(e)})
    else:
        return JsonResponse({"error": "Invalid request method."})

@csrf_exempt
def chatCompletion(request):
    if (request.method == "POST"):
        userMessage = request.POST["userMessage"]

        print(type(userMessage))

        messages.append({"role": "user", "content": userMessage})

        tts = TTS("tts_models/en/ljspeech/tacotron2-DDC")
        
        if not userMessage:
            return JsonResponse({"error": "No chat data provided"})
        try:
            

            chatResponse = openai.ChatCompletion.create(
                model = "gpt-3.5-turbo",
                messages=messages
            )
            botResponse = chatResponse['choices'][0]['message']['content']

            messages.append({"role": "assistant", "content": botResponse})
            # I have tried to directly send the generated audio data to the frontend without saving a local file,
            # only to find that it's really troublesome..

            # audio = tts.tts(botResponse, speaker=tts.speakers[0], language=tts.languages[0])
            # audioNp = np.array(audio)
            # audioNp = audioNp.reshape((-1, 1))
            # audioBytes = audioNp.tobytes()
            # audioBase64 = base64.b64encode(audioBytes).decode("utf-8")
            # response = HttpResponse(audioBase64, content_type="audio/wav")
            timestamp = datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
            filename = "output" + timestamp + ".wav"
            
            tts.tts_to_file(botResponse, file_path=os.path.join(outputPath, filename))
            
            response = JsonResponse({"botResponse": botResponse, "filename": filename})

            # response.headers['X-JSON'] = json.dumps({"botResponse": botResponse})
            # response.headers['Access-Control-Expose-Headers'] = "X-JSON"
            
            return response
        except openai.InvalidRequestError as e:
            return JsonResponse({"error": "Error during making chat completion: {}".format(e)})
    else:
        return JsonResponse({"error": "Invalid request method."})
    
@csrf_exempt
def checkFile(request):
    if(request.method == "POST"):
        filename = request.POST["filename"]
        filepath = os.path.join(outputPath, filename)
        if(os.path.exists(filepath)):
            return HttpResponse("File exists.")
        else:
            return HttpResponse("File does not exist.")
    else:
        return JsonResponse({"error": "Invalid request method."})
    
