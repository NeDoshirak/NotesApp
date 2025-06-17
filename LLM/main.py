from flask import Flask, request, jsonify, render_template
import os
import requests
import json
from vosk import Model, KaldiRecognizer
import wave
from datetime import datetime
from flasgger import Swagger

app = Flask(__name__)

# === Загрузка модели Vosk ===
vosk_model = Model("vosk-model-small-ru-0.22")

# === Настройки API ===
API_URL = "https://openrouter.ai/api/v1/chat/completions"
API_KEY = "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"

current_date = datetime.now().strftime("%Y-%m-%d")
current_time = datetime.now().strftime("%H:%M")

headers = {
    "Authorization": f"Bearer {API_KEY}",
    "Content-Type": "application/json"
}

# === Системный промпт ===
system_prompt = f"""
Ты — интеллектуальный помощник, задача которого — структурировать пользовательские заметки в формате JSON.

Пользователь вводит произвольный текст с личным напоминанием, задачей или планом (например: "завтра в 10 утра записаться к врачу").

Твоя задача:
1. Проанализировать текст и извлечь ключевые детали;
2. Преобразовать их в строго следующий JSON-формат:

{{
  "title": "Краткое название заметки (1–5 слов)",
  "text": "Переформулированный текст заметки. ",
  "category": "Работа / Личное / Обучение / Здоровье / Покупки",
  "date_time": "ГГГГ-ММ-ДД , ЧЧ:ММ",  // Дата, если она упоминается, иначе null
  "location": Локацию, если она упоминается, 
}}

Правила:
- Используй контекстные подсказки: например, "завтра" → дата, "в 10 утра" → время, "к врачу" → место: Клиника.
- Если нет явных данных — заполни значением по умолчанию.
- Не добавляй комментарии, пояснения, символы или текст до или после JSON.
- Возвращай строго один корректный JSON-объект.

📅 Сегодняшняя дата: {current_date}
🕒 Текущее время: {current_time}
"""


def process_with_api(text):
    data = {
        "model": "meta-llama/llama-3.3-8b-instruct:free",
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": text}
        ]
    }
    return requests.post(API_URL, headers=headers, json=data)

@app.route("/upload_wav", methods=["POST"])
def process():
    """
       Распознавание аудио или обработка текста и структурирование заметки
       ---
       consumes:
         - multipart/form-data
         - text/plain
         - application/json
       parameters:
         - name: audio
           in: formData
           type: file
           required: false
           description: WAV-файл (PCM 16bit mono)
         - name: text
           in: body
           required: false
           schema:
             type: object
             properties:
               text:
                 type: string
                 example: "Позвонить маме завтра в 18:00"
       responses:
         200:
           description: Успешный ответ
           schema:
             type: object
             properties:
               transcribed_text:
                 type: string
               result:
                 type: object
         400:
           description: Ошибка в запросе
         500:
           description: Ошибка сервера или API
       """
    content_type = request.headers.get('Content-Type', '').lower()

    if 'text/plain' in content_type or 'application/json' in content_type:
        try:
            text = request.json.get('text', '') if request.is_json else request.data.decode('utf-8')
            if not text.strip():
                return jsonify({"error": "Empty text provided"}), 400
            response = process_with_api(text)
        except Exception as e:
            return jsonify({"error": f"Text processing error: {str(e)}"}), 400

    elif 'multipart/form-data' in content_type:
        if 'audio' not in request.files:
            return jsonify({"error": "Нет файла 'audio' в запросе"}), 400

        audio_file = request.files['audio']
        temp_file = "temp_audio.wav"

        try:
            audio_file.save(temp_file)

            # Проверка WAV
            with wave.open(temp_file, "rb") as wf:
                if (wf.getnchannels() != 1 or
                        wf.getsampwidth() != 2 or
                        wf.getcomptype() != "NONE"):
                    return jsonify({"error": "WAV должен быть PCM 16bit mono"}), 400

                rec = KaldiRecognizer(vosk_model, wf.getframerate())
                rec.SetWords(True)
                result_text = ""

                while True:
                    data = wf.readframes(4000)
                    if len(data) == 0:
                        break
                    if rec.AcceptWaveform(data):
                        result_text += json.loads(rec.Result()).get("text", "") + " "

                result_text += json.loads(rec.FinalResult()).get("text", "")

            os.remove(temp_file)

            if not result_text.strip():
                return jsonify({"error": "Пустой результат распознавания"}), 400

            response = process_with_api(result_text)

        except Exception as e:
            if os.path.exists(temp_file):
                os.remove(temp_file)
            return jsonify({"error": f"Audio processing error: {str(e)}"}), 400
    else:
        return jsonify({"error": "Unsupported Content-Type. Use text/plain or audio/wav"}), 400

    if response.status_code == 200:
        try:
            content = response.json()['choices'][0]['message']['content']
            json_start = content.find('{')
            json_end = content.rfind('}') + 1
            clean_json = content[json_start:json_end]
            parsed = json.loads(clean_json)

            return jsonify({
                "transcribed_text": result_text if 'result_text' in locals() else text,
                "result": parsed
            })
        except Exception as e:
            return jsonify({
                "error": "API response parsing failed",
                "details": str(e),
                "raw_response": content if 'content' in locals() else None
            }), 500
    else:
        return jsonify({
            "error": "API request failed",
            "status_code": response.status_code,
            "response_text": response.text
        }), response.status_code

@app.route("/parse_note", methods=["POST"])
def parse_note():
    """
    Обработка текстовой заметки и структурирование её в JSON
    ---
    consumes:
      - application/json
    parameters:
      - name: note
        in: body
        required: true
        schema:
          type: object
          properties:
            note:
              type: string
              example: "Записаться к стоматологу на пятницу в 10:30"
    responses:
      200:
        description: Успешный ответ
        schema:
          type: object
      400:
        description: Пустой ввод
      500:
        description: Ошибка сервера или API
    """
    try:
        user_input = request.json.get("note", "")
        if not user_input.strip():
            return jsonify({"error": "Empty input"}), 400

        response = process_with_api(user_input)
        if response.status_code == 200:
            result = response.json()
            content = result['choices'][0]['message']['content']

            json_start = content.find('{')
            json_end = content.rfind('}') + 1
            clean_json = content[json_start:json_end]

            parsed_json = json.loads(clean_json)
            return jsonify(parsed_json)
        else:
            return jsonify({"error": "API error", "status": response.status_code}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route("/")
def index():
    return render_template("index.html")

if __name__ == "__main__":
    swagger = Swagger(app)
    app.run(host="0.0.0.0", port=5000, debug=True)
