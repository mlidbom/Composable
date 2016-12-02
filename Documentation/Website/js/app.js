
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
                return i.href == document.location.href
            }).each(function (_, elem) {
                console.log(elem)
                $(elem).parent().addClass("active")
            })
    }
)
