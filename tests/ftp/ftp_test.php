<?php
// Enviroment variables PEACHPIE_FTP_TEST_SERVER, PEACHPIE_FTP_TEST_USER, PEACHPIE_FTP_TEST_PASSWORD must be setted
$server = $user_name = $user_pass = false;

if (!Init()){
   echo 'Init failed';
  exit(0);
}

if (!Test1($server, $user_name,$user_pass)){
   echo 'Test1 failed';
   exit(0);
}

if (!Test2($server, $user_name,$user_pass)){
   echo 'Test2 failed';
   exit(0);
}

if (!Test3($server, $user_name, $user_pass)){
    echo 'Test3 failed';
    exit(0);
}

//Test4($server, $user_name, $user_pass);

function Init() {
    global $server, $user_name,$user_pass;
    $server = getenv('PEACHPIE_FTP_TEST_SERVER');
    $user_name = getenv('PEACHPIE_FTP_TEST_USER');
    $user_pass = getenv('PEACHPIE_FTP_TEST_PASSWORD');
    $testingFileClient = 'TestingFileClient.txt';
    $testingFileServer = 'TestingFileServer.txt';

    if (files_are_equal($testingFileClient, 'TestingFileClientCopy.txt')){
        echo "success\n";
    }

    if (!$server || !$user_name || !$user_pass){
        echo "Enviroment were not found\n";
        return false;
    }

    if(!$conn_id = ftp_connect($server)){
        echo "Connect failed\n";
        return false;
    }
    if ( !ftp_login($conn_id, $user_name, $user_pass)) { 
        return false;
    }

    // prepare server
    // Test1,3
    if (!ftp_put($conn_id, $testingFileServer, $testingFileServer)) {
        return false;
    }
    // Test1,3
    if (ftp_size($conn_id, $testingFileClient) != -1) { 
        if (!ftp_delete($conn_id, $testingFileClient, $testingFileClient)) {
            return false;
        }
    }
    // Directories Test2
    if(@ftp_chdir($conn_id,'MyDirectory')){
        if(@ftp_chdir($conn_id,'InnerDirectory')){
            @ftp_chdir($conn_id, "..");
            if (ftp_rmdir($conn_id,'InnerDirectory')){
                echo "InnerDirectory deleted\n";
            }
            else{
                return false;
            }
        }
        if(@ftp_chdir($conn_id,'NewDirector')){
            @ftp_chdir($conn_id, "..");
            if (ftp_rmdir($conn_id,'NewDirector')){
                echo "NewDirector deleted\n";
            }
            else{
                return false;
            }
        }
        @ftp_chdir($conn_id, "..");
        if (ftp_rmdir($conn_id,'MyDirectory')){
            echo "MyDirectory deleted\n";
        }
        else{
            return false;
        }
    }

    ftp_close($conn_id);

    return true;
}

function Test1($server, $user_name, $user_pass) {
    //Testing ftp_connect, ftp_login, ftp_close, ftp_pwd, ftp_get_option, ftp_size, ftp_rawlist, ftp_nlist
    // variables
    $ftp_fakeServer = '	0.0.0.0';
    $wrong_user_name = 'someNamewhichprobablydoesntexistonserverforthistest';
    $wrong_user_pass = 'somesillypasswordwhichisverycomplicatedandwierdforuser';

    // ftp_connect
    $conn_right = ftp_connect($server);

    $conn_wrong = @ftp_connect($ftp_fakeServer);

    if (!$conn_right) {
        echo "ftp_connect method failed";
        return false;
    }

    // ftp_login
    print_r(@ftp_login($conn_wrong, $user_name, $user_pass)); //fail
    print_r(@ftp_login($conn_right, $wrong_user_name, $wrong_user_pass)); //fail

    if ( !ftp_login($conn_right, $user_name, $user_pass)) { //success
        echo "ftp_login method failed";
        return false;
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

    // logging...
    $conn_id = ftp_connect($server); 
    if (ftp_login($conn_id, $user_name, $user_pass)){
        echo "logged on\n";
    }
    else
    {
        return false;
    }

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

    return ftp_close($conn_id) ? true : false;
}

function Test3($server, $user_name, $user_pass){
    // Testing ftp_fput, ftp_put, ftp_fget, ftp_delete, ftp_site, ftp_pasv

    // Variables
    $testingFileClient = 'TestingFileClient.txt';
    $testingFileServer = 'TestingFileServer.txt';

    // logging...
    $conn_id = ftp_connect($server); 
    if (ftp_login($conn_id, $user_name, $user_pass)){
        echo "logged on\n";
    } else {
        return false;
    }

    if (ftp_pasv($conn_id, true)){
        echo "pasv on\n";
    }

    // ftp_put
    if (ftp_put($conn_id,$testingFileClient,$testingFileClient)){
        echo "File was sent to server\n";
    } else {
        echo "File was not sent to server\n";
    }

    if (ftp_pasv($conn_id, false)){
    echo "pasv off\n";
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

    if (@ftp_site($conn_id, "CHMOD 0600 $testingFileServer")) {
        echo "Command executed successfully.\n";
    } else {
        echo "Command failed\n";
    }

    if (files_are_equal($testingFileClient, 'TestingFileClientCopy.txt')){
        echo "success\n";
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
        echo ftp_pwd($conn_id);
    }

    ftp_close($conn_id);
}