<?php
namespace web\parse_url; 

function test()
{
    $urls = array(
        'http://subdomain.host.com/path?',
        'http://subdomain.host.com/path?#arg1=1&q=arg2.com',
        'http://www.somehost.com//',
		'http://www.somehost.com////',
        'http:\\a.b.c\d\e\f?x');
    
    foreach($urls as $url)
    {
        $url_parts = parse_url($url);
        print_r($url_parts);
    }    
}
test();

?>