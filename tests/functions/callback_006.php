<?php
namespace functions\callback_006;

class X
{
	function foo() { echo __METHOD__ ."\n"; }
}

class Y extends X
{
  function foo() { echo __METHOD__ ."\n"; }

  function parentFoo() { call_user_func([$this, "parent::foo"]); }
}

class Z extends Y
{
  function foo() { echo __METHOD__ ."\n"; }

  function parentFoo() { call_user_func([$this, "parent::foo"]); }
}

function test()
{
  $x = new X;
  call_user_func([$x, "foo"]);
  call_user_func([$x, __NAMESPACE__ ."\\X::foo"]);

  $y = new Y;
  call_user_func([$y, "foo"]);
  call_user_func([$y, __NAMESPACE__ ."\\X::foo"]);
  $y->parentFoo();
  call_user_func([$y, __NAMESPACE__ ."\\Y::foo"]);

  $z = new Z;
  call_user_func([$z, "foo"]);
  call_user_func([$z, __NAMESPACE__ ."\\X::foo"]);
  call_user_func([$z, __NAMESPACE__ ."\\Y::foo"]);
  $z->parentFoo();
  call_user_func([$z, __NAMESPACE__ ."\\Z::foo"]);
}

test();
