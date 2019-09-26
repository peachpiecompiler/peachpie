<?php
// Testing ftp_ssl_connect

// Variables
$ftp_server = getenv('peachpie_ftp_server');

$ftp_user_name = getenv('peachpie_ftp_user');
$ftp_user_pass = getenv('peachpie_ftp_password');

$directory = 'MyDirectory';
$innerDirectory = 'InnerDirectory';
$newDirectory = 'NewDirectory';

// logging...
$conn_id = ftp_ssl_connect($ftp_server); 

if (ftp_login($conn_id, $ftp_user_name, $ftp_user_pass)){
echo ftp_pwd($conn_id);
}

ftp_close($conn_id);