<?php

function test(float /*|double|*/$page, float /*|double|*/$max_pages)
{
  if ( /*|double|*/$page > 1 ) {
    /*|double|*/$prev_page = /*|double|*/$page - 1;

    if ( /*|double|*/$prev_page > /*|double|*/$max_pages ) {
      /*|double|*/$prev_page = /*|double|*/$max_pages;
    }

    echo /*|double|*/$prev_page;
  }
}
