<?php
//Testing ftp_connect, ftp_login, ftp_close, ftp_pwd, ftp_get_option, ftp_size, ftp_rawlist, ftp_nlist
// Enviroment variables peachpie_ftp_server, peachpie_ftp_user, peachpie_ftp_password must be setted


// variables
$ftp_server = getenv('peachpie_ftp_server');
$ftp_fakeServer = '111.111.111.111';

$ftp_right_user_name = getenv('peachpie_ftp_user');
$ftp_right_user_pass = getenv('peachpie_ftp_password');

$ftp_wrong_user_name = 'hacker';
$ftp_wrong_user_pass = 'password';

if (!$ftp_server || !$ftp_right_user_name || !$ftp_right_user_pass){
    echo "Enviroment variables was not found";
    exit(0);
}

// ftp_connect
$conn_right = ftp_connect($ftp_server);

$conn_wrong = ftp_connect($ftp_fakeServer);

if (!$conn_right) {
    echo "ftp_connect method failed";
    exit();
}

// ftp_login

print_r(@ftp_login($conn_wrong, $ftp_right_user_name, $ftp_right_user_pass)); //fail

print_r(@ftp_login($conn_right, $ftp_wrong_user_name, $ftp_wrong_user_pass)); //fail

if ( !ftp_login($conn_right, $ftp_right_user_name, $ftp_right_user_pass)) { //success
    echo "ftp_login method failed";
    exit();
}

//ftp_get_option
echo "Connection information\n";
echo "TIMEOUT: " . ftp_get_option($conn_right,FTP_TIMEOUT_SEC) . "\n";
echo "AUTOSEEK " . ftp_get_option($conn_right,FTP_AUTOSEEK) . "\n";

// ftp_pwd

$pwd = ftp_pwd($conn_right);
echo "Working directory: " . $pwd . "\n";

// ftp_size
$file = "TestingFileServer.txt";

echo "Size: " . ftp_size($conn_right, $pwd) . "\n";
echo "Size of Test.txt: " . ftp_size($conn_right, $file) . "\n";

// ftp_rawlist

print_r(ftp_rawlist($conn_right,$pwd,true));
echo "\n";

// ftp_nlist

print_r(ftp_nlist($conn_right,$pwd));
echo "\n";

// close the connection and the file handler
if (!ftp_close($conn_right)){
    echo "ftp_close method failed"; 
}