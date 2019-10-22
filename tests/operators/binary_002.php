<?php
namespace operators\binary_002;

function a(){

}

function c(){
    return a() * 5;
}

echo(c());


function d(){
	return 2 - a();
}

echo(d());

function g(){
	return a() - a();
}

echo(g());