<?php
// Testing ftp_rename, ftp_mdtm, ftp_chmod, ftp_mkdir, ftp_rmdir, ftp_chdir 

// Variables
$ftp_server = getenv('peachpie_ftp_server');

$ftp_user_name = getenv('peachpie_ftp_user');
$ftp_user_pass = getenv('peachpie_ftp_password');

$directory = 'MyDirectory';
$innerDirectory = 'InnerDirectory';
$newDirectory = 'NewDirectory';

// logging...
$conn_id = ftp_connect($ftp_server); 
if (ftp_login($conn_id, $ftp_user_name, $ftp_user_pass)){
    echo 'logged on';
} 
else
    echo 'logged off';

// ftp_mkdir
echo "Created directory: " . ftp_mkdir($conn_id, $directory) . "\n";

// ftp_chdir
ftp_chdir($conn_id,$directory);
echo "Working Directory: " . ftp_pwd($conn_id) . "\n";

// ftp_chmod
echo "Created directory: " . ftp_mkdir($conn_id, $innerDirectory) . "\n";
if (@ftp_chmod($conn_id, 0777, $innerDirectory) !== false) {
    echo "$innerDirectory chmoded successfully to 644\n";
   } 
else {
    echo "could not chmod $innerDirectory\n";   }
    echo date_default_timezone_set ( 'Europe/Prague' );
// ftp_mdtm
$buff = ftp_mdtm($conn_id, $innerDirectory);
if ($buff != -1) {
     //somefile.txt was last modified on: March 26 2003 14:16:41.
    echo "$innerDirectory was last modified on : " . date("F d Y H:i:s.", $buff) . "\n";
    //echo "$innerDirectory was last modified on : " . $buff . "\n";
} else {
   echo "Couldn't get mdtime\n";
}

// ftp_rename
if (ftp_rename($conn_id, $innerDirectory, $newDirectory)) {
    echo "ftp_rename ok\n";
}
else {
    echo "ftp_rename failed\n";
}

//rm dir
if (ftp_rmdir($conn_id, $newDirectory)) {
    echo "ftp_rmdir ok\n";
}
else {
    echo "ftp_rmdir failed\n";
}
ftp_chdir($conn_id, "..");
echo "Working Directory: " . ftp_pwd($conn_id) . "\n";
if (ftp_rmdir($conn_id, $directory)) {
    echo "ftp_rmdir ok\n";
}
else {
    echo "ftp_rmdir failed\n";
}
// ftp_systype
echo ftp_systype($conn_id);

ftp_close($conn_id);