<?php
namespace hash\password_get_info_001;

// TODO: Update PASSWORD_* constants and interface according to PHP 7.4 breaking change in
//       https://wiki.php.net/rfc/password_registry so that it's not necessary
function print_info($info) {
  switch ($info['algo']) {
    case PASSWORD_BCRYPT:
      $info['algo'] = "PASSWORD_BCRYPT";
      break;
    case PASSWORD_ARGON2I:
      $info['algo'] = "PASSWORD_ARGON2I";
      break;
    case PASSWORD_ARGON2ID:
      $info['algo'] = "PASSWORD_ARGON2ID";
      break;
    default:
      if (!$info['algo']) {
        $info['algo'] = "uknown";
      }
      break;
  }

  print_r($info);
}

$password = "rasmuslerdorf";
$salt = "Ajnbu298IRHUVa56XvDOzu";
$memory_cost = 512;
$time_cost = 10;
$threads = 6;

$options = [
    'threads' => $threads,
    'time_cost' => $time_cost,
    'memory_cost' => $memory_cost,
    'cost' => $time_cost,
    'salt' => $salt,
];

$hashBCrypt = @password_hash($password,PASSWORD_DEFAULT,$options);
$hashArgon2ID = @password_hash( $password, PASSWORD_ARGON2ID, $options);
$hashArgon2I= @password_hash( $password, PASSWORD_ARGON2I, $options);

print_info(password_get_info(NULL));
print_info(password_get_info("UnknownAlgorithm"));
print_info(password_get_info($hashBCrypt));
print_info(password_get_info($hashArgon2ID));
print_info(password_get_info($hashArgon2I));

echo "Done.";
