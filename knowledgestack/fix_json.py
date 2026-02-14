import os

file_path = r"c:\Users\tugka\knowledgestack\knowledgestack\Assets\Resources\questions.json"

with open(file_path, "rb") as f:
    f.seek(-300, 2) # Read last 300 bytes
    tail = f.read()

print(f"Tail bytes: {tail}")

# Attempt to fix: Find the last valid closing brace '}' of an object and truncate there
content = tail.decode('utf-8', errors='ignore')
last_brace_index = content.rfind('}')

if last_brace_index != -1:
    # We found the last '}', but we need to check if it's the very last one (file end) or an object end
    # The file structure is { "questions": [ { ... }, { ... } ] }
    # So we expect the sequence: object '}', array ']', root '}'
    
    # Let's try to reconstruct the end safely.
    # We will read the whole file, string manip to find the last object ending, and close it.
    
    with open(file_path, "r", encoding="utf-8") as f:
        full_content = f.read()
    
    # Find the last "difficulty" key, which is present in all objects
    last_diff = full_content.rfind('"difficulty"')
    if last_diff != -1:
        # Find the closing brace after this
        end_of_obj = full_content.find('}', last_diff)
        if end_of_obj != -1:
            # Truncate everything after this }
            new_content = full_content[:end_of_obj+1] + "\n  ]\n}"
            
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(new_content)
            print("File fixed successfully via truncation and closure.")
        else:
            print("Could not find closing brace after last difficulty.")
    else:
        print("Could not find 'difficulty' key.")
else:
    print("No closing brace found in tail.")
