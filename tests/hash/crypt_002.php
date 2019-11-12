<?php

echo crypt('rasmuslerdorf', '$6$rounds=5000$usesomesillystringforsalt$');
echo crypt('password', '$6$rounds=5000$salt$');
echo crypt('password', '$6$rounds=5000$$');
echo crypt('password', '$6$rounds=5000$');
echo crypt('password', '$6$');
