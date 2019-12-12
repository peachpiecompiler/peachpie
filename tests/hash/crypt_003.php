<?php
// Tests sha256 crypt
echo crypt('rasmuslerdorf', '$5$rounds=5000$usesomesillystringforsalt$');
echo crypt('password', '$5$rounds=2000$salt$');
echo crypt('password', '$5$rounds=3030$$');
echo crypt('password', '$5$rounds=6000$');
echo crypt('password', '$5$');
