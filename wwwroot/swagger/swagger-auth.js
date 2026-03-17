(function () {
  const nativeFetch = window.fetch.bind(window);

  async function getSignedHeaders(method, path) {
    const url = `/swagger/auth-headers?method=${encodeURIComponent(method)}&path=${encodeURIComponent(path)}`;
    const response = await nativeFetch(url, { credentials: "same-origin" });
    if (!response.ok) {
      return null;
    }

    return response.json();
  }

  window.fetch = async function (input, init) {
    const request = input instanceof Request ? input : new Request(input, init || {});
    const url = new URL(request.url, window.location.origin);

    const isApiRequest = url.pathname.startsWith("/api/");
    const isWebhook = url.pathname.startsWith("/api/Webhook");

    if (!isApiRequest || isWebhook) {
      return nativeFetch(input, init);
    }

    const method = (request.method || "GET").toUpperCase();
    const path = url.pathname;
    const signed = await getSignedHeaders(method, path);

    if (!signed) {
      return nativeFetch(input, init);
    }

    const headers = new Headers(request.headers || (init && init.headers) || {});
    headers.set("X-API-KEY", signed.apiKey);
    headers.set("CLIENT_ID", signed.clientId);
    headers.set("X-TIMESTAMP", signed.timestamp);
    headers.set("X-SIGNATURE", signed.signature);

    const signedRequest = new Request(request, { headers });
    return nativeFetch(signedRequest);
  };
})();
