<?php
namespace hash\sha3_512_001;

function test_sha3_512($init, $hmac = true)
{
  echo "\nsha3-512, incremental: ";
  $h = hash_init('sha3-512');
  for($i=0;$i<10;++$i) hash_update($h, '' . $init*2 + $i*17);
  echo '(copying state) ';
  $h2 = hash_copy($h);
  for($i=0;$i<10;++$i) hash_update($h, '' . $init*2 + $i*19);
  print_r(hash_final($h));

  echo "\nsha3-512, from copied state: ";
  print_r(hash_final($h2));

  echo "\nsha3-512, HMAC, incremental: ";
  $h = hash_init('sha3-512', $hmac ? HASH_HMAC : 0, 'HMAC key. It can be very long, but in this case it will be rehashed to fit the block size of the hashing algorithm...'.$init*147);
  for($i=0;$i<10;++$i) hash_update($h, '' . $init*4 + $i*7);
  echo '(copying state) ';
  $h2 = hash_copy($h);
  for($i=0;$i<10;++$i) hash_update($h, '' . $init*3 + $i*11);
  print_r(hash_final($h));

  echo "\nsha3-512, from copied state, HMAC: ";
  print_r(hash_final($h2));

  echo "\nsha3-512, at once, short data: ";
  print_r(hash('sha3-512', 'some string to be hashed ... ' . $init * 123 . ' ...'));

  if ($hmac)
  {
    echo "\nsha3-512, at once, HMAC: ";
    print_r(hash_hmac('sha3-512', 'some string to be hashed ... ' . $init * 123 . ' ...', 'HMAC key. It can be very long, but in this case it will be rehashed to fit the block size of the hashing algorithm.'));
  }
}

echo "sha3-512 in hash_algos: ";
var_dump(in_array('sha3-512', hash_algos()));

test_sha3_512(12345678, false);
test_sha3_512(64983042165);
