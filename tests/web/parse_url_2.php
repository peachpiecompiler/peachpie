<?php
namespace web\parse_url_2;

print_r(parse_url('www.example.com/multimedia/item_list.php?something=1'));
print_r(parse_url('www.example.com:12345/multimedia/item_list.php?is=1'));
print_r(parse_url('www.example.com:98765/multimedia/item_list.php?pos=1'));
print_r(parse_url('file://C:/windows/php.ini'));
print_r(parse_url('mailto:info@example.com?subject=[test]'));
print_r(parse_url('hxxp://domain.com?query#fragment'));
print_r(parse_url('/include/file.php?xx=1'));
print_r(parse_url('http://domain:123/path1/path2#fragment'));
print_r(parse_url('something:/domain:123/path1/path2#fragment'));
print_r(parse_url('/directory/multimedia/item_list.php?id=1#fragment'));
print_r(parse_url('http://user:password@example.com/path1/path2/item_list.php?id=1$#position'));
print_r(parse_url('https://[2001:0db8:85a3:08d3:1319:8a2e:0370:7344]:443/dir/dir/dir/dir/file.aspx?param1#frag'));

?>