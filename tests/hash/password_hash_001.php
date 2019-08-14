<?php
/**Test password_hash blowfish algorithm */
$password = "rasmuslerdorf";
$salt = "Ajnbu298IRHUVa56XvDOzu";
$cost = 15;
$options =[
    'cost' => 15,
    'salt' => $salt,
];

$hashD = @password_hash($password,PASSWORD_DEFAULT,$options);
$hashB = @password_hash($password,PASSWORD_BCRYPT,$options);

echo "PASSWORD_DEFAULT : $hashD\n";
echo "PASSWORD_BCRYPT : $hashB\n";
?>