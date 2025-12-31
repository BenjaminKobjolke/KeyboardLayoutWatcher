d:
cd "d:\GIT\BenjaminKobjolke\GPT-json-translator"

call activate_environment.bat

call .\.venv\Scripts\python.exe json_translator.py "D:\GIT\BenjaminKobjolke\KeyboardLayoutWatcher\lang\en.json"

cd %~dp0
