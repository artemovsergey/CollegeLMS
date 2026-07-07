"""
Голосовой ввод: запись → Google Speech API → текст в консоль + буфер обмена.
Использование: python voice_google.py
"""

import sounddevice as sd
import numpy as np
import scipy.io.wavfile as wav
import speech_recognition as sr
import pyperclip
import tempfile
import os
import sys
import re

SAMPLE_RATE = 16000
DEVICE_ID = 2
RECORD_SECONDS = 10


def clean_text(text):
    text = re.sub(r'\[.*?\]', '', text)
    text = re.sub(r'\(.*?\)', '', text)
    text = re.sub(r'\s+', ' ', text)
    return text.strip()


def main():
    print(f"Запись {RECORD_SECONDS} секунд... Говорите!")
    audio = sd.rec(int(RECORD_SECONDS * SAMPLE_RATE), samplerate=SAMPLE_RATE, channels=1, dtype='float32', device=DEVICE_ID)
    sd.wait()
    print("Запись завершена. Распознаю...")

    audio = audio.flatten()
    peak = np.max(np.abs(audio))
    if peak < 0.001:
        print("(тихо - нет звука)")
        return

    audio = audio / max(peak, 0.01)

    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as f:
        wav.write(f.name, SAMPLE_RATE, (audio * 32767).astype(np.int16))
        wav_path = f.name

    recognizer = sr.Recognizer()
    with sr.AudioFile(wav_path) as source:
        audio_data = recognizer.record(source)
    os.unlink(wav_path)

    try:
        text = recognizer.recognize_google(audio_data, language="ru-RU")
        text = clean_text(text)
        if text:
            pyperclip.copy(text)
            print(f"\n--- ТЕКСТ ---\n{text}\n--------------")
            print("(скопировано в буфер обмена)")
        else:
            print("(пусто)")
    except sr.UnknownValueError:
        print("(не распознано)")
    except sr.RequestError as e:
        print(f"(ошибка сети: {e})")


if __name__ == "__main__":
    main()
