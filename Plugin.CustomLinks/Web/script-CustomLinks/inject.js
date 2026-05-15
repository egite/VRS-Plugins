if(VRS && VRS.LinkRenderHandler && VRS.linkRenderHandlers) {
    (function() {
        var request = new XMLHttpRequest();
        request.open('GET', '/CustomLinks/GetLinks.json', false);
        request.send(null);

        if(request.status === 200) {
            var links = JSON.parse(request.responseText);
            for(var i = 0; i < links.length; ++i) {
                (function(index, link) {
                    var siteName = 'CustomLink_' + index;
                    VRS.LinkSite[siteName] = siteName.toLowerCase();

                    VRS.linkRenderHandlers.push(
                        new VRS.LinkRenderHandler({
                            linkSite:           VRS.LinkSite[siteName],
                            displayOrder:       10000 + index,
                            canLinkAircraft:    function(aircraft) {
                                var url = link.Url || '';
                                if(url.indexOf('{icao}') !== -1     && !aircraft.formatIcao())           return false;
                                if(url.indexOf('{reg}') !== -1      && !aircraft.formatRegistration())   return false;
                                if(url.indexOf('{callsign}') !== -1 && !aircraft.formatCallsign(false))  return false;
                                return true;
                            },
                            hasChanged:         function(aircraft) {
                                return aircraft.icao.chg || aircraft.registration.chg || aircraft.callsign.chg;
                            },
                            title:              function(aircraft) {
                                return link.Name || 'Custom Link';
                            },
                            buildUrl:           function(aircraft) {
                                var url = link.Url || '';
                                url = url.replace(/\{icao\}/g,     encodeURIComponent(aircraft.formatIcao()          || ''));
                                url = url.replace(/\{reg\}/g,      encodeURIComponent(aircraft.formatRegistration()  || ''));
                                url = url.replace(/\{callsign\}/g, encodeURIComponent(aircraft.formatCallsign(false) || ''));
                                return url;
                            },
                            target:             'customLink-' + index
                        })
                    );
                })(i, links[i]);
            }
        }
    })();
}
