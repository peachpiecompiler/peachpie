<?php
namespace hash\password_hash_001;
/**Test password_hash blowfish algorithm */
$password = '';
$salt = "Ajnbu298IRHUVa56XvDOzu";
$options =[
    'cost' => 10,
    'salt' => $salt,
];

$hashD = @password_hash($password,PASSWORD_DEFAULT,$options);
$hashB = @password_hash($password,PASSWORD_BCRYPT,$options);

$options['salt'] = "aa";
$hashshortsalt = @password_hash($password,PASSWORD_DEFAULT,$options);

$options['salt'] = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
$hashlongsalt = @password_hash($password,PASSWORD_DEFAULT,$options);

$options['salt'] = $salt;
$hashPasswordNull = @password_hash(null,PASSWORD_DEFAULT,$options);

$options['cost'] = 10.2;
$hashdouble =  @password_hash($password,PASSWORD_DEFAULT,$options);

$options['cost'] = "10";
$hashstring =  @password_hash($password,PASSWORD_DEFAULT,$options);

echo "PASSWORD_DEFAULT : $hashD\n";
echo "PASSWORD_BCRYPT : $hashB\n";
echo "PASSWORD_BCRYPT : $hashshortsalt\n";
echo "PASSWORD_BCRYPT : $hashlongsalt\n";
echo "PASSWORD_BCRYPT : $hashPasswordNull \n";
echo "PASSWORD_BCRYPT : $hashdouble\n";
echo "PASSWORD_BCRYPT : $hashstring\n";
