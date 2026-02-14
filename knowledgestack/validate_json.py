import json
import sys

try:
    with open(r"c:\Users\tugka\knowledgestack\knowledgestack\Assets\Resources\questions.json", "r", encoding="utf-8") as f:
        data = json.load(f)
    print("JSON is valid.")
except json.JSONDecodeError as e:
    print(f"JSON Error: {e.msg}")
    print(f"Line: {e.lineno}")
    print(f"Column: {e.colno}")
except Exception as e:
    print(f"Error: {e}")
