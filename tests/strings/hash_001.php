<?php
namespace strings\hash_001;

function test_hash($algo, $init, $hmac = true)
{
  echo "\n$algo, incremental: ";
  $h = hash_init($algo);
  for($i=0;$i<10;++$i) hash_update($h, '' . $init*2 + $i*17);
  echo '(copying state) ';
  $h2 = hash_copy($h);
  for($i=0;$i<10;++$i) hash_update($h, '' . $init*2 + $i*19);
  print_r(hash_final($h));

  echo "\n$algo, from copied state: ";
  print_r(hash_final($h2));

  echo "\n$algo, HMAC, incremental: ";
  $h = hash_init($algo, $hmac ? HASH_HMAC : 0, 'HMAC key. It can be very long, but in this case it will be rehashed to fit the block size of the hashing algorithm...'.$init*147);
  for($i=0;$i<10;++$i) hash_update($h, '' . $init*4 + $i*7);
  echo '(copying state) ';
  $h2 = hash_copy($h);
  for($i=0;$i<10;++$i) hash_update($h, '' . $init*3 + $i*11);
  print_r(hash_final($h));

  echo "\n$algo, HMAC, from copied state: ";
  print_r(hash_final($h2));

  echo "\n$algo, at once, short data: ";
  print_r(hash($algo, 'some string to be hashed ... ' . $init * 123 . ' ...'));

  if ($hmac)
  {
    echo "\n$algo, at once, HMAC: ";
    print_r(hash_hmac($algo, 'some string to be hashed ... ' . $init * 123 . ' ...', 'HMAC key. It can be very long, but in this case it will be rehashed to fit the block size of the hashing algorithm.'));
  }
}

test_hash('adler32', 12345678, false);
test_hash('crc32', 2345678, false);
test_hash('crc32b', 345678, false);
test_hash('md2', 45678);
test_hash('md4', 5678);
test_hash('md5', 678);
test_hash('sha1', 111222);
test_hash('sha256', 64983042165);
test_hash('sha512', 64983042165);
// add more tests when other hashing algorithms are implemented
