
$(document).ready(
    function(){

        $(".page-toc nav ul").first().each( //Find the root UL in the generated toc
            function (index, elem) {
                $(elem).attr("data-magellan","") //Make it a magellan menu
            }
        )

        $(".page-toc a[href^='#']").each(//find each link in the magellan menu
            function (theTag, tocLink) {
                var tag = $($(tocLink).attr("href")) //Find the element targetted by the menu link
                tag.attr("data-magellan-target", tag.attr("id"))//Mark it with the attribute needed by magellan to highlight the correct link
            }
        )

        $(document).foundation()

        $(".top-bar ul.dropdown.menu li a")
            .filter(function(e, i) {

                var current_base_url = document.location.href.indexOf('#') > -1 ?
                    document.location.href.substr(0, document.location.href.indexOf('#'))
                    : document.location.href;
                return i.href == current_base_url
            }).each(function (_, elem) {
                console.log(elem)
                $(elem).parent().addClass("active")
            })
    }
)
