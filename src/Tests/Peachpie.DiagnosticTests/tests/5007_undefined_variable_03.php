<?php

function get_taxonomy($taxonomy) {
  global $wp_taxonomies;

  if (!isset($wp_taxonomies[$taxonomy]))
    return false;

  return $wp_taxonomies[$taxonomy];
}

function foo($sth) {
  return true;
}

function bar() {
  $taxonomy = get_taxonomy("1");

  echo $taxonomy;

  if (!foo($taxonomy)) {
    echo $taxonomy;
  }

  echo $taxonomy;
}
