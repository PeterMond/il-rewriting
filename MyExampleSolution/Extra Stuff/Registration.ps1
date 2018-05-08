Clear

cd "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7 Tools\x64\"
.\sn.exe –i "C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\MyCPlusPlusProject\x64\Release\MyCPlusPlusProject.snk"
.\gacutil.exe /i "C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\MyCPlusPlusProject\x64\Release\MyCPlusPlusProject.dll"


cd 	"C:\Windows\System32\"
.\regsvr32 /i "C:\Users\tbowen\Desktop\MyExampleSolution\MyExampleSolution\MyCPlusPlusProject\x64\Release\MyCPlusPlusProject.dll"