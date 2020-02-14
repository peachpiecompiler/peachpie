<?php
namespace ftp\ftp_test;
// Enviroment variables containing credentials must be set
if (!getenv('PEACHPIE_FTP_TEST_SERVER') || !getenv('PEACHPIE_FTP_TEST_USER') || !getenv('PEACHPIE_FTP_TEST_PASSWORD')){
  exit("***SKIP***");
}

$server = $user_name = $user_pass = false;

if (!Init()){
    echo "Initilazation failed\n";
    exit(0);
}

echo "Initialized\n";

if (!Test1($server, $user_name,$user_pass)){
 echo "Test1 failed\n";
 exit(0);
}

echo "Test1 passed\n";

if (!Test2($server, $user_name,$user_pass)){
 echo "Test2 failed\n";
 exit(0);
}

echo "Test2 passed\n";

if (!Test3($server, $user_name, $user_pass)){
  echo 'Test3 failed';
 exit(0);
}

echo "Test3 passed\n";

Test4($server, $user_name, $user_pass);

Test5($server, $user_name, $user_pass);

Test6($server, $user_name, $user_pass);

function Init() {
    global $server, $user_name,$user_pass;
    $server = getenv('PEACHPIE_FTP_TEST_SERVER');
    $user_name = getenv('PEACHPIE_FTP_TEST_USER');
    $user_pass = getenv('PEACHPIE_FTP_TEST_PASSWORD');

    // Checking enviroment variables
    if (!$server || !$user_name || !$user_pass){
        echo "Enviroment variables were not found\n";
        return false;
    }
    else {
        echo "Enviroment variables were found\n";
    }

    // Log in to server
    if(!$conn_id = Login($server,$user_name,$user_pass)){
        return false;
    }

    ftp_close($conn_id);

    return true;
}

function Login($server,$user_name,$user_pass) {
    echo "Login...\n";

    if(!$conn_id = ftp_connect($server)){
        echo "Can not connect to server\n";
        return false;
    }
    else{
        echo "You are connect to server\n";
    }

    if ( !ftp_login($conn_id, $user_name, $user_pass)) {
        echo "Can not login in to server\n";
        return false;
    }
    else{
        echo "You are logged in to server\n";
    }

    if (ftp_pasv($conn_id, true)) {
      echo "Passive mode set\n";
    } else {
      echo "Cannot set passive mode\n";
    }

    return $conn_id;
}

function Test1($server, $user_name, $user_pass) {
    //Testing ftp_connect, ftp_login, ftp_close, ftp_pwd, ftp_get_option, ftp_size, ftp_delete
    // Variables
    $ftp_fakeServer = '	0.0.0.0';
    $wrong_user_name = 'someNamewhichprobablydoesntexistonserverforthistest';
    $wrong_user_pass = 'somesillypasswordwhichisverycomplicatedandwierdforuser';
    $testingFileServer = 'TestingFileServer.txt';

    // ftp_connect
    @Login($ftp_fakeServer,$wrong_user_name,$wrong_user_pass);
    @Login($server,$wrong_user_name,$wrong_user_pass);
    $conn_right = Login($server,$user_name,$user_pass);

    if (!$conn_right){
        return false;
    }

    // Prepare server
    if (!ftp_put($conn_right, $testingFileServer, $testingFileServer)) {
        echo "Testing file $testingFileServer was not uploaded\n";
        return false;
    }

    echo "Testing file $testingFileServer was uploaded\n";

    //ftp_get_option
    echo "Connection information\n";
    echo "TIMEOUT: " . ftp_get_option($conn_right,FTP_TIMEOUT_SEC) . "\n";
    echo "AUTOSEEK " . ftp_get_option($conn_right,FTP_AUTOSEEK) . "\n";

    // ftp_pwd
    $pwd = ftp_pwd($conn_right);
    echo "Working directory: " . $pwd . "\n";

    // ftp_size
    echo "Size of working directory: " . ftp_size($conn_right, $pwd) . "\n";
    echo "Size of $testingFileServer: " . ftp_size($conn_right, $testingFileServer) . "\n";

    // ftp_delete
    ftp_delete($conn_right,$testingFileServer);

    // Close the connection
    if (!ftp_close($conn_right)){
        echo "ftp_close method failed";
        return false;
    }

    return true;
}

function Test2($server, $user_name, $user_pass) {
    // Testing ftp_rename, ftp_mdtm, ftp_chmod, ftp_mkdir, ftp_rmdir, ftp_chdir
    // Variables
    $directory = 'MyDirectory';
    $innerDirectory = 'InnerDirectory';
    $newDirectory = 'NewDirectory';

    // Login...
    if (!$conn_id = Login($server,$user_name,$user_pass)){
        return false;
    }

    // Prepare server
    if(@ftp_chdir($conn_id,'MyDirectory')){
        if(@ftp_chdir($conn_id,$innerDirectory)){
            @ftp_chdir($conn_id, "..");
            if (ftp_rmdir($conn_id,$innerDirectory)){
                echo "$innerDirectory deleted\n";
            }
            else{
                echo "$innerDirectory could not be deleted\n";
                return false;
            }
        }
        if(@ftp_chdir($conn_id,$newDirectory)){
            @ftp_chdir($conn_id, "..");
            if (ftp_rmdir($conn_id,$newDirectory)){
                echo "$newDirectory deleted\n";
            }
            else{
                echo "$newDirectory could not be deleted\n";
                return false;
            }
        }
        @ftp_chdir($conn_id, "..");
        if (ftp_rmdir($conn_id, $directory)){
            echo "$directory deleted\n";
        }
        else{
            echo "$directory could not be deleted\n";
            return false;
        }
    }

    // ftp_mkdir
    echo "Created directory: " . ftp_mkdir($conn_id, $directory) . "\n";
    // ftp_chdir
    ftp_chdir($conn_id,$directory);
    echo "Working Directory: " . ftp_pwd($conn_id) . "\n";

    // ftp_chmod
    echo "Created directory: " . ftp_mkdir($conn_id, $innerDirectory) . "\n";
    if (@ftp_chmod($conn_id, 0777, $innerDirectory) !== false) {
        echo "$innerDirectory chmoded successfully to 0777\n";
    }
    else {
        echo "could not chmod $innerDirectory\n";   }

    date_default_timezone_set ( 'Europe/Prague' );

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

    return ftp_close($conn_id) ? true : false;
}

function Test3($server, $user_name, $user_pass){
    // Testing ftp_fput, ftp_put, ftp_fget, ftp_delete, ftp_site, ftp_pasv

    // Variables
    $testingFileClient = 'TestingFileClient.txt';
    $testingFileServer = 'TestingFileServer.txt';
    $testingFileClientCopy = 'TestingFileClientCopy.txt';

    // Login...
    if (!$conn_id = Login($server,$user_name,$user_pass)){
        return false;
    }


    // Prepare server
    if (ftp_size($conn_id, $testingFileClient) != -1) {
        if (!ftp_delete($conn_id, $testingFileClient)) {
            return false;
        }
    }

    if (ftp_size($conn_id, $testingFileServer) != -1) {
        if (!ftp_delete($conn_id, $testingFileServer)) {
            return false;
        }
    }

    if (ftp_put($conn_id,$testingFileServer,$testingFileServer)){
        echo "File $testingFileServer was sent to server\n";
    } else {
        echo "File $testingFileServer was not sent to server\n";
        return false;
    }

    if (ftp_pasv($conn_id, false)){
        echo "pasv off\n";
    }

    if (ftp_pasv($conn_id, true)){
        echo "pasv on\n";
    }

    // ftp_put
    if (ftp_put($conn_id,$testingFileClient,$testingFileClient)){
        echo "File $testingFileClient was sent to server\n";
    } else {
        echo "File $testingFileClient was not sent to server\n";
    }

    // ftp_fget
    $handle = fopen($testingFileClient, 'w');

    if (ftp_fget($conn_id, $handle, $testingFileServer)) {
        echo "successfully written to $testingFileClient\n";
    } else {
        echo "There was a problem while downloading $testingFileServer to $testingFileClient\n";
    }

    fclose($handle);

    $handle = fopen($testingFileClient, 'w');

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
        echo "File $testingFileClient was sent to server\n";
    } else {
    echo "File $testingFileClient was not sent to server\n";
    }

    fclose($fp);

    if (ftp_delete($conn_id, $testingFileClient)) {
        echo "$testingFileClient deleted successful\n";
    } else {
        echo "could not delete $testingFileClient\n";
    }

    // ftp_site

    if (@ftp_site($conn_id, "CHMOD 0600 $testingFileServer")) {
        echo "Command executed successfully.\n";
    } else {
        echo "Command failed\n";
    }

    if (files_are_equal($testingFileClient, $testingFileClientCopy)){
        echo "success\n";
    }

    if (ftp_size($conn_id, $testingFileServer) != -1) {
        if (!ftp_delete($conn_id, $testingFileServer)) {
            echo "File $testingFileServer was not deleted in server\n";
            return false;
        }
        echo "File $testingFileServer was deleted in server\n";
    }

    ftp_close($conn_id);
    return true;
}

function files_are_equal($a, $b) {
  // Check if filesize is different
  if(filesize($a) !== filesize($b))
      return false;

  // Check if content is different
  $ah = fopen($a, 'rb');
  $bh = fopen($b, 'rb');

  $result = true;
  while(!feof($ah))
  {
    if(fread($ah, 8192) != fread($bh, 8192))
    {
      $result = false;
      break;
    }
  }

  fclose($ah);
  fclose($bh);

  return $result;
}

function Test4($server, $user_name, $user_pass){
    // Testing ftp_ssl_connect
    // logging...
    $conn_id = ftp_ssl_connect($server);

    if (ftp_login($conn_id, $user_name, $user_pass)){
        echo "SSL-FTP connection successful\n";
        echo "Working directory: ". ftp_pwd($conn_id) ."\n";
    } else {
        echo "Error in establishing  SSL-FTP connection\n";
    }

    ftp_close($conn_id);
}

function Test5($server, $user_name, $user_pass){
    $directory = 'MyDirectory';

    // Login...
    if (!$conn_id = Login($server,$user_name,$user_pass)){
        return false;
    }

    // Prepare server
    if(@ftp_chdir($conn_id,'MyDirectory')){
        @ftp_chdir($conn_id, "..");
        if (ftp_rmdir($conn_id, $directory)){
            echo "$directory deleted\n";
        }
        else{
            echo "$directory could not be deleted\n";
            return false;
        }
    }

    // ftp_rawlist
    print_r(ftp_rawlist($conn_id, '/', true));
    echo "\n";

    // ftp_nlist
    print_r(ftp_nlist($conn_id, '/'));
    echo "\n";

    if (ftp_mkdir($conn_id, $directory)){
        echo "Created directory: " .  $directory . "\n";
    }

    if (@ftp_chdir($conn_id, $directory)){
        echo "Change directory: " . ftp_pwd($conn_id) . "\n";
    }

    if (ftp_rmdir($conn_id, "../" . $directory)){
        echo "$directory deleted\n";
    }
    else{
        echo "$directory could not be deleted\n";
        return false;
    }

    // ftp_mlsd
    print_r(ftp_mlsd($conn_id,ftp_pwd($conn_id)));

    // ftp_raw
    print_r(ftp_raw($conn_id,"PWD"));

    echo ftp_alloc($conn_id,1,$answer);
    echo $answer;

    ftp_close($conn_id);
}

function Test6($server, $user_name, $user_pass){
    // Test no-blocking functions: ftp_nb-continue/get/put/fget/fput

    $fileName = 'TestingFile.txt';
    $fileName1 = 'TestingFile1.txt';

    // Login...
    if (!$conn_id = Login($server,$user_name,$user_pass)){
        return false;
    }

    if (!GenerateFile($fileName))
    {
        echo "Could not create testing file\n";
        return false;
    }

    //ftp_nb_put
    $writeOnlyOnce=false;
    // Initate the upload
    $ret = ftp_nb_put($conn_id, $fileName, $fileName, FTP_BINARY);
    while ($ret == FTP_MOREDATA) {

    // Do whatever you want
    if (!$writeOnlyOnce)
    {
        echo "Async function in the background..\n";
        $writeOnlyOnce = true;
    }

    // Continue uploading...
    $ret = ftp_nb_continue($conn_id);
    }

    if ($ret != FTP_FINISHED) {
    echo "There was an error uploading the file...";
    exit(1);
    }

    //ftp_nb_get
    $writeOnlyOnce=false;
    // Initate the upload
    $ret = ftp_nb_get($conn_id, $fileName, $fileName, FTP_BINARY);
    while ($ret == FTP_MOREDATA) {

    // Do whatever you want
    if (!$writeOnlyOnce)
    {
        echo "Async function in the background..\n";
        $writeOnlyOnce = true;
    }

    // Continue downloading...
    $ret = ftp_nb_continue($conn_id);
    }

    if ($ret != FTP_FINISHED) {
    echo "There was an error downloading the file...";
    exit(1);
    }

    //Delete generated files
    ftp_delete($conn_id, $fileName);
    @ftp_delete($conn_id, $fileName1);
    @unlink($fileName);

    ftp_close($conn_id);
}
function GenerateFile($fileName):bool{
    $newFile = fopen($fileName,'w');
    if (!$newFile){
        return false;
    }

    for($i = 0; $i<3000; $i++)
    {
        fwrite($newFile,$i);
    }

    fclose($newFile);
    return true;
}