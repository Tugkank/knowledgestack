
with open(r"c:\Users\tugka\knowledgestack\knowledgestack\Assets\Resources\questions.json", "r", encoding="utf-8") as f:
    lines = f.readlines()
    start = max(0, 5605)
    end = min(len(lines), 5615)
    for i in range(start, end):
        print(f"{i+1}: {lines[i].rstrip()}")
