// Scroll to hash
// automatically smooth-scrolls to whatever the hash element is on a given page

function scrollTo(hash) {
  var element = document.getElementById(hash);
  if (element) {
    scrollToResolver(document.getElementById(hash));
  }
}

function scrollToResolver(elem) {
  var jump = parseInt(elem.getBoundingClientRect().top * .2);
  document.body.scrollTop += jump;
  document.documentElement.scrollTop += jump;
  //lastjump detects anchor unreachable, also manual scrolling to cancel animation if scroll > jump
  if (!elem.lastjump || elem.lastjump > Math.abs(jump)) {
    elem.lastjump = Math.abs(jump);
    setTimeout(function() {
      ScrollToResolver(elem);
    }, "100");
  } else {
    elem.lastjump = null;
  }
}

document.addEventListener('DOMContentLoaded', function() {
  if (window.location.hash)
  {
    scrollTo(window.location.hash);
  }
}, false);
