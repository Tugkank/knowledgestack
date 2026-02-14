import json

file_path = r"c:\Users\tugka\knowledgestack\knowledgestack\Assets\Resources\questions.json"

with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Find the start of the questions array
start_index = content.find('"questions":')
if start_index == -1:
    print("Error: 'questions' key not found.")
    exit(1)

array_start = content.find('[', start_index)
if array_start == -1:
    print("Error: Array start '[' not found.")
    exit(1)

print(f"Array starts at index {array_start}")

# Iterate through content to find the matching closing bracket
balance = 0
in_string = False
escape = False
cutoff_index = -1

for i in range(array_start, len(content)):
    char = content[i]
    
    if escape:
        escape = False
        continue
        
    if char == '\\':
        escape = True
        continue
        
    if char == '"':
        in_string = not in_string
        continue
        
    if not in_string:
        if char == '[':
            balance += 1
        elif char == ']':
            balance -= 1
            if balance == 0:
                cutoff_index = i
                break

if cutoff_index != -1:
    print(f"Found array end at index {cutoff_index}")
    # We found the end of the array. The file should end shortly after with '}'
    # But if there is garbage after, we truncate.
    
    # Check if there's a closing brace for the root object
    root_end = content.find('}', cutoff_index)
    if root_end != -1:
        print(f"Found root end at index {root_end}")
        new_content = content[:root_end+1]
    else:
        print("Root closing brace missing. Appending it.")
        new_content = content[:cutoff_index+1] + "\n}"
        
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(new_content)
    print("File cropped to valid JSON structure.")

else:
    print("Array not closed. Attempting to close safely.")
    # If not closed, find the last '}' (end of last object) and close there
    last_obj_end = content.rfind('}')
    if last_obj_end != -1:
        # This might be unsafe if the file is garbage, but better than nothing
        # Ensure we are inside the array
        if last_obj_end > array_start:
             new_content = content[:last_obj_end+1] + "\n  ]\n}"
             with open(file_path, "w", encoding="utf-8") as f:
                f.write(new_content)
             print("File forcefully closed after last object.")
    else:
        print("Could not recover JSON.")
