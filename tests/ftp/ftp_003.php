<?php
// Testing ftp_fput, ftp_put, ftp_fget, ftp_delete, ftp_site, ftp_pasv

// Variables
$ftp_server = getenv('peachpie_ftp_server');

$ftp_user_name = getenv('peachpie_ftp_user');
$ftp_user_pass = getenv('peachpie_ftp_password');
$testingFileClient = 'TestingFileClient.txt';
$testingFileServer = 'TestingFileServer.txt';

// logging...
$conn_id = ftp_connect($ftp_server); 
if (ftp_login($conn_id, $ftp_user_name, $ftp_user_pass)){
    echo "logged on\n";
} else {
    echo "logged off\n";
}

if (ftp_pasv($conn_id, true)){
    echo "pasv on";
}

// ftp_put
if (ftp_put($conn_id,$testingFileClient,$testingFileClient)){
    echo "File was sent to server\n";
} else {
echo "File was not sent to server\n";
}

if (ftp_pasv($conn_id, false)){
   echo "pasv off";
}

// ftp_fget
$handle = fopen($testingFileClient, 'w');

if (ftp_fget($conn_id, $handle, $testingFileServer)) {
    echo "successfully written to $testingFileClient\n";
} else {
    echo "There was a problem while downloading $testingFileServer to $testingFileClient\n";
}

if (ftp_fget($conn_id, $handle, $testingFileClient)) {
    echo "successfully written to $testingFileClient\n";
} else {
    echo "There was a problem while downloading $testingFileClient to $testingFileClient\n";
}

fclose($handle);

// ftp_delete
if (ftp_delete($conn_id, $testingFileClient)) {
    echo "$testingFileClient deleted successful\n";
} else {
    echo "could not delete $testingFileClient\n";
}

// ftp_fput
$fp = fopen($testingFileClient, 'r');

if (ftp_fput($conn_id, $testingFileClient, $fp)){
    echo "File was sent to server\n";
} else {
echo "File was not sent to server\n";
}

fclose($fp);

if (ftp_delete($conn_id, $testingFileClient)) {
    echo "$testingFileClient deleted successful\n";
} else {
    echo "could not delete $testingFileClient\n";
}

// ftp_site

if (ftp_site($conn_id, "CHMOD 0600 $testingFileServer")) {
    echo "Command executed successfully.\n";
 } else {
    echo 'Command failed.';
 }
ftp_close($conn_id);