<?php
namespace hash\password_get_info_002;

function test()
{
    print_r(password_get_info(password_hash("foo", 1)));
    print_r(password_get_info(password_hash("foo", "1")));      // Does not work
    print_r(password_get_info(password_hash("foo", "2y")));
    print_r(password_get_info(password_hash("foo", PASSWORD_BCRYPT)));
}

test();
