<?php
namespace com_dotnet\com;

function test() {
    if (PHP_OS != "WINNT") {
        exit("***SKIP***");
    }

    if (!extension_loaded("com_dotnet")) {
        echo "Extension com_dotnet not loaded";
        return;
    }

    echo "testing fso" .PHP_EOL;
    $fso = new \COM('Scripting.FileSystemObject');
    echo !empty($fso->GetTempName()) . PHP_EOL;
    // echo $fso->GetSpecialFolder(0) . PHP_EOL; // <- Fails (COM_Object)
    $drives = $fso->Drives;
    echo gettype($drives).PHP_EOL;
    foreach($drives as $d) {
       //echo $d.PHP_EOL; // <- Fails (COM_Object)
       $dO = $fso->GetDrive($d); 
       //echo $dO->DriveLetter.PHP_EOL; // <- Fails (COM_Object)
       break;
    }

    echo "testing Shell" .PHP_EOL;
    $shell = new \COM('WScript.Shell');
    echo $shell->CurrentDirectory . PHP_EOL;
    $shell->CurrentDirectory = "C:\\";
    echo $shell->CurrentDirectory . PHP_EOL;
}

test();
