<?php
// Tests md5 crypt
echo crypt('rasmuslerdorf', '$1$rasmusle$') . '\n';
echo crypt('aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', '$1$verylongsalt$') . '\n';
echo crypt('a', '$1$salt$') . '\n';