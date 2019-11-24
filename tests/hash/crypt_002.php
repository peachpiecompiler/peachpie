<?php
// Tests sha512 crypt
echo crypt('rasmuslerdorf', '$6$rounds=5000$usesomesillystringforsalt$');
echo crypt('password', '$6$rounds=2000$salt$');
echo crypt('password', '$6$rounds=3030$$');
echo crypt('password', '$6$rounds=6000$');
echo crypt('password', '$6$');
