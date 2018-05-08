$AssemblerDirectoryPath          = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319'
$DisassemblerDirectoyPath        = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools'

$ExampleTestsDLLOriginalFilePath = 'C:\Users\tbowen\Desktop\Binaries\Original.dll'
$ExampleTestDLLFilePath          = 'C:\Users\tbowen\Desktop\Binaries\Example.Tests.dll'
$ExampleTestILFilePath           = 'C:\Users\tbowen\Desktop\Binaries\Example.Tests.il'
$ExampleTestPDBFilePath          = 'C:\Users\tbowen\Desktop\Binaries\Example.Tests.pdb'
 
Clear
                                 
Write-Host '-------------------------------------------------------------------'
Write-Host 'Generate IL File --------------------------------------------------'
Write-Host '-------------------------------------------------------------------'
CD $DisassemblerDirectoyPath
.\ildasm.exe $ExampleTestsDLLOriginalFilePath /OUT=$ExampleTestILFilePath /SOURCE

Write-Host '-------------------------------------------------------------------'
Write-Host 'Modify IL File ----------------------------------------------------'
Write-Host '-------------------------------------------------------------------'

Write-Host '-------------------------------------------------------------------'
Write-Host 'Generate New DLL File ---------------------------------------------'
Write-Host '-------------------------------------------------------------------'
CD $AssemblerDirectoryPath
.\ilasm.exe $ExampleTestILFilePath /DLL /OUT=$ExampleTestDLLFilePath

Write-Host '-------------------------------------------------------------------'
Write-Host 'Generate New PDB File ---------------------------------------------'
Write-Host '-------------------------------------------------------------------'
CD $AssemblerDirectoryPath
.\ilasm.exe /DLL $ExampleTestILFilePath /DEBUG