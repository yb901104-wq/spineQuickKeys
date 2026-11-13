/* ------------------------------------------------------------------
 * Spine Export Preview - JS bridge (host.html <-> WinForms)
 * All asset URLs are same-origin under https://spinepreview.local/ and are
 * served by C# through CoreWebView2.WebResourceRequested (S3 route).
 * No file:// access, no copying of export files.
 *
 * Stage-0 verified against @esotericsoftware/spine-player 4.2.120.
 * ------------------------------------------------------------------ */
(function () {
  "use strict";

  var player = null;
  var host = null;
  var lastTick = 0;
  var needPose = false;
  var disposedCount = 0;
  var currentAnimation = "";
  var currentSkin = "default";
  var loop = true;
  var metadataSent = false;
  var viewportFixed = false;
  var bootstrapTimer = null;
  var PHYS = (window.spine && spine.Physics && spine.Physics.update) || 0;

  // ── AUD-043 ③④⑤：视图状态 ──────────────────────────────────────────
  // 官方 SpinePlayer 从未实例化 CameraController（三个内置 runtime 中
  // `new CameraController` 均为 0 次），且 drawFrame() 每帧按 currentViewport
  // 重算 camera.zoom / camera.position。因此缩放与平移必须落在
  // player.currentViewport 上，在每帧绘制前重新应用，而不是直接改 camera。
  var view = { zoom: 1, panX: 0, panY: 0 };
  var baseViewport = null;      // 该文件首次加载时官方算出的基准包围盒
  var viewportLocked = false;   // 锁定后切动画不再重建 currentViewport
  var viewInputBound = false;
  var bgHex = "1E1E20";
  var lastBleed = false;

  function post(obj) {
    try {
      if (window.chrome && window.chrome.webview)
        window.chrome.webview.postMessage(obj);
    } catch (e) { /* host gone */ }
  }

  function currentEntry() {
    try {
      return player && player.animationState
        ? player.animationState.getCurrent(0) : null;
    } catch (e) { return null; }
  }

  function glInfo() {
    var count = document.querySelectorAll("canvas").length;
    var lost = null;
    try {
      var c = host && host.querySelector("canvas");
      if (c) {
        var gl = c.getContext("webgl2") || c.getContext("webgl");
        lost = gl ? gl.isContextLost() : null;
      }
    } catch (e) { /* ignore */ }
    return { canvasCount: count, webglLost: lost, disposedCount: disposedCount };
  }

  function emitMetadata() {
    if (!player || !player.skeleton) return;
    var data = player.skeleton.data;
    var skins = (data.skins || []).map(function (s) { return s.name; });
    var anims = (data.animations || []).map(function (a) { return a.name; });
    var entry = currentEntry();
    var info = glInfo();
    currentAnimation = entry && entry.animation ? entry.animation.name : "";
    currentSkin = player.skeleton.skin ? player.skeleton.skin.name : "default";
    post({
      type: "ready",
      skins: skins,
      animations: anims,
      duration: entry && entry.animation ? entry.animation.duration : 0,
      skin: currentSkin,
      animation: currentAnimation,
      canvasCount: info.canvasCount,
      webglLost: info.webglLost,
      disposedCount: info.disposedCount
    });
  }

  function onFrame(p, delta) {
    // AUD-043 ③：success 时 canvas 可能尚未挂载，首帧做一次幂等兜底。
    installView(p);
    if (!viewInputBound) bindViewInput();
    // 4.1.x quirk: the official post-success branch guards viewport setup with
    // `else if (!this.currentViewport)`, but currentViewport starts as {} (truthy).
    // If track 0 was already installed before success(), setViewport() is skipped,
    // the camera gets NaN coordinates and the canvas stays blank even though
    // ready/metadata/tick all look healthy. By the first frame track 0 is always
    // installed, so repair the viewport here. 4.2/4.3 already have a valid
    // currentViewport.x and are left untouched.
    if (!viewportFixed && p.skeleton && p.animationState) {
      viewportFixed = true;
      try {
        var cv = p.currentViewport;
        var badViewport = !cv || !isFinite(cv.x) || !isFinite(cv.y)
                          || !isFinite(cv.width) || !isFinite(cv.height)
                          || !isFinite(cv.padLeft);
        var e0 = p.animationState.getCurrent(0);
        if (badViewport && typeof p.setViewport === "function" && e0 && e0.animation)
          p.setViewport(e0.animation);
      } catch (e) { /* best effort */ }
    }
    // config.success() fires before track 0 is installed. The first frame
    // callback runs after the official post-success branch, so metadata is
    // emitted here (duration / current animation are then reliable).
    if (!metadataSent && p.skeleton && p.animationState) {
      metadataSent = true;
      emitMetadata();
    }
    // drawFrame() keeps requesting frames while paused but skips
    // animationState.apply(). Re-apply here so seek() is visible when paused.
    if (p.paused && needPose) {
      needPose = false;
      try { p.animationState.apply(p.skeleton); } catch (e) { }
      try { p.skeleton.updateWorldTransform(PHYS); } catch (e) { }
    }
    var now = performance.now();
    if (now - lastTick < 100) return;
    lastTick = now;
    var entry = currentEntry();
    if (!entry || !entry.animation) return;
    post({
      type: "tick",
      trackTime: entry.trackTime,
      duration: entry.animation.duration,
      paused: !!p.paused
    });
  }

  // ── AUD-043 ③：视图变换 ─────────────────────────────────────────────
  function canvasEl() { return host ? host.querySelector("canvas") : null; }

  function viewMetrics() {
    if (!baseViewport) return null;
    var cv = player && player.currentViewport;
    var c = canvasEl();
    if (!cv || !c) return null;
    var pl = isFinite(cv.padLeft) ? cv.padLeft : 0;
    var pr = isFinite(cv.padRight) ? cv.padRight : 0;
    var pt = isFinite(cv.padTop) ? cv.padTop : 0;
    var pb = isFinite(cv.padBottom) ? cv.padBottom : 0;
    return {
      w: baseViewport.width + pl + pr,
      h: baseViewport.height + pt + pb,
      cw: c.clientWidth || 1,
      ch: c.clientHeight || 1
    };
  }

  // 从 baseViewport 幂等重算，绝不基于上一帧的 currentViewport 累加。
  function applyView() {
    if (!player || !baseViewport) return;
    var cv = player.currentViewport;
    if (!cv) return;
    var w = baseViewport.width / view.zoom;
    var h = baseViewport.height / view.zoom;
    var cx = baseViewport.x + baseViewport.width / 2;
    var cy = baseViewport.y + baseViewport.height / 2;
    cv.x = cx - w / 2 + view.panX;
    cv.y = cy - h / 2 + view.panY;
    cv.width = w;
    cv.height = h;
  }

  function bindViewInput() {
    if (viewInputBound) return;
    var c = canvasEl();
    if (!c) return;
    viewInputBound = true;
    try { c.style.touchAction = "none"; } catch (e) { }

    c.addEventListener("wheel", function (ev) {
      if (ev.preventDefault) ev.preventDefault();
      var mt = viewMetrics();
      if (!mt) return;
      var rect = c.getBoundingClientRect();
      var nx = (ev.clientX - rect.left) / (rect.width || 1);
      var ny = 1 - (ev.clientY - rect.top) / (rect.height || 1);
      var z0 = view.zoom;
      var z1 = Math.min(10, Math.max(0.1, z0 * Math.exp(-ev.deltaY * 0.0015)));
      if (Math.abs(z1 - z0) < 1e-6) return;
      // 以光标下的世界点为锚：缩放前后该点保持不动。
      view.panX += (nx - 0.5) * mt.w * (1 / z0 - 1 / z1);
      view.panY += (ny - 0.5) * mt.h * (1 / z0 - 1 / z1);
      view.zoom = z1;
    }, { passive: false });

    var dragging = false, lastX = 0, lastY = 0;
    c.addEventListener("pointerdown", function (ev) {
      if (ev.button !== 0) return;
      dragging = true; lastX = ev.clientX; lastY = ev.clientY;
      try { c.setPointerCapture(ev.pointerId); } catch (e) { }
    });
    c.addEventListener("pointermove", function (ev) {
      if (!dragging) return;
      var mt = viewMetrics();
      if (!mt) return;
      var dx = ev.clientX - lastX, dy = ev.clientY - lastY;
      lastX = ev.clientX; lastY = ev.clientY;
      view.panX -= dx / mt.cw * mt.w / view.zoom;
      view.panY += dy / mt.ch * mt.h / view.zoom;
    });
    function endDrag() { dragging = false; }
    c.addEventListener("pointerup", endDrag);
    c.addEventListener("pointercancel", endDrag);
    c.addEventListener("pointerleave", endDrag);
  }

  // ── AUD-043 ④：锁定基准视图 ────────────────────────────────────────
  // 官方 setAnimation() 首行 `animation = this.setViewport(animation)`，
  // setViewport 会重新赋值 currentViewport 并按该动画包围盒重算，还会启动
  // previousViewport 过渡。这里包装它：首次（以及 viewport 为 NaN 的场景，
  // 即 AUD-042 的 4.1 空白画布修复）放行，之后只返回 animation 不改视图。
  function installView(p) {
    if (!p || p.__eapViewInstalled) return;
    p.__eapViewInstalled = true;

    var origSetViewport = p.setViewport;
    p.setViewport = function (anim) {
      if (!viewportLocked) {
        var r = origSetViewport.call(p, anim);
        var cv = p.currentViewport;
        if (cv && isFinite(cv.x) && isFinite(cv.y)
                 && isFinite(cv.width) && isFinite(cv.height)) {
          baseViewport = { x: cv.x, y: cv.y, width: cv.width, height: cv.height };
          viewportLocked = true;
          p.previousViewport = null;
        }
        return r;
      }
      // 已锁定：setAnimation() 会把返回值当 animation 对象使用，字符串需解析。
      if (typeof anim === "string" && p.skeleton && p.skeleton.data)
        return p.skeleton.data.findAnimation(anim) || anim;
      return anim;
    };

    var origDrawFrame = p.drawFrame;
    p.drawFrame = function () {
      applyView();
      return origDrawFrame.apply(p, arguments);
    };

    bindViewInput();
  }

  function startBootstrapPump() {
    // SpinePlayer.loadSkeleton() is driven by requestAnimationFrame. Chromium
    // suspends rAF when the WebView is occluded / backgrounded / not focused,
    // which can leave loading stuck forever. Pump drawFrame(false) ourselves
    // (no extra rAF scheduling) until the skeleton is ready, then stop.
    if (bootstrapTimer) clearInterval(bootstrapTimer);
    bootstrapTimer = setInterval(function () {
      if (!player) return;
      if (player.skeleton || player.error) {
        clearInterval(bootstrapTimer);
        bootstrapTimer = null;
        return;
      }
      try { player.drawFrame(false); } catch (e) { }
    }, 120);
  }

  function loadGroup(m) {
    try {
      if (bootstrapTimer) { clearInterval(bootstrapTimer); bootstrapTimer = null; }
      if (player) {
        try { player.dispose(); } catch (e) { }
        player = null;
        disposedCount++;
      }
      var stage = document.getElementById("stage");
      while (stage.firstChild) stage.removeChild(stage.firstChild);
      host = document.createElement("div");
      stage.appendChild(host);

      var g = m.group || {};
      loop = m.loop !== false;
      metadataSent = false;
      viewportFixed = false;
      // AUD-043 ③④：切换文件必须重置用户视图；锁定状态与基准包围盒一起清空。
      view = { zoom: 1, panX: 0, panY: 0 };
      baseViewport = null;
      viewportLocked = false;
      viewInputBound = false;
      lastBleed = !!g.bleed;
      bgHex = g.backgroundColor || bgHex;
      // 配置字段跨版本兼容（三个内置版本线各取所需，值相同不会重复加载）：
      //   4.1.x  validateConfig 只认 jsonUrl / binaryUrl + atlasUrl
      //   4.2.x  skeleton / atlas 优先，同时兼容旧字段
      //   4.3.x  只认 skeleton / atlas
      // 磁盘文件名可能是 Unity 的 .skel.bytes，但 C# 已把 URL 规范化为
      // skeleton.json / skeleton.skel，因此按结尾判格式是可靠的。
      var skeletonUrl = g.skeleton || "";
      var isJsonSkeleton = /\.json$/i.test(skeletonUrl);
      var cfg = {
        skeleton: skeletonUrl,
        jsonUrl: isJsonSkeleton ? skeletonUrl : undefined,
        binaryUrl: isJsonSkeleton ? undefined : skeletonUrl,
        atlas: g.atlas,
        atlasUrl: g.atlas,
        showControls: false,
        showLoading: false,
        premultipliedAlpha: g.premultipliedAlpha !== false,
        preserveDrawingBuffer: false,
        backgroundColor: bgHex,
        alpha: false,
        interactive: false,
        mipmaps: true,
        defaultMix: m.mix === undefined ? 0.25 : m.mix,
        scale: 1,
        success: function (p) {
          player = p;
          // AUD-043 ④：success() 在官方后续分支调用 setViewport() 之前触发，
          // 此刻包装 setViewport 才能捕获到「首次」基准包围盒并锁定视图。
          installView(p);
          // Official success() fires BEFORE track 0 is installed. Do NOT call
          // setAnimation() here (viewport/renderer not ready yet -> throws and
          // leaves an empty animation). Instead write the preferred animation
          // back to config; the official branch right after success() installs
          // it, and the first frame() callback then emits reliable metadata.
          if (!g.animation) {
            // Prefer "idle", then the first animation with a positive duration,
            // then simply the first. Some Spine animations (e.g. aim) are
            // single-frame poses with duration 0, which makes the timeline useless.
            var anims = p.skeleton.data.animations || [];
            var first = null, firstTimed = null, firstIdle = null;
            for (var i = 0; i < anims.length; i++) {
              if (!first) first = anims[i].name;
              if (!firstTimed && anims[i].duration > 0) firstTimed = anims[i].name;
              if (!firstIdle && /idle/i.test(anims[i].name) && anims[i].duration > 0)
                firstIdle = anims[i].name;
            }
            var chosen = firstIdle || firstTimed || first;
            if (chosen) { try { p.config.animation = chosen; } catch (e) { } }
          }
          // Pick a skin with actual attachments. Many production projects keep
          // "default" nearly empty and put all regions in named skins; under 4.1
          // that leaves every slot without an attachment at viewport-calculation
          // time, producing NaN camera values and a permanently blank canvas.
          try {
            var wantedSkin = g.skin;
            if (!wantedSkin) {
              var allSkins = p.skeleton.data.skins || [];
              var bestCount = -1;
              for (var si = 0; si < allSkins.length; si++) {
                var cnt = 0;
                try {
                  var am = allSkins[si].attachments;
                  if (am) cnt = Object.keys(am).length;
                } catch (e2) { cnt = 0; }
                if (cnt > bestCount) { bestCount = cnt; wantedSkin = allSkins[si].name; }
              }
            }
            if (wantedSkin) {
              p.skeleton.setSkinByName(wantedSkin);
              p.skeleton.setSlotsToSetupPose();
            }
          } catch (e) { /* keep whatever skin the runtime set up */ }
          if (m.speed !== undefined) p.speed = m.speed;
          if (m.autoPlay !== false) p.play();
          needPose = true;
        },
        error: function (p, msg) {
          post({ type: "error", phase: "load", reason: String(msg) });
        },
        frame: onFrame
      };
      if (g.animation) cfg.animation = g.animation;
      if (g.skin) cfg.skin = g.skin;
      player = new spine.SpinePlayer(host, cfg);
      // 有意保留的发布版只读诊断接口：供 harness / 真机排查跨版本渲染问题
      // （空皮肤、NaN viewport、WebGL 丢失等）读取 player 内部状态。
      // 仅暴露对象引用，不提供远程入口，也不写回任何导出源文件。
      try { window.__eapPlayer = player; window.__eapCfg = cfg; } catch (e) { }
      startBootstrapPump();
    } catch (e) {
      post({ type: "error", phase: "construct", reason: String(e && e.message || e) });
    }
  }

  function handle(m) {
    try {
      switch (m.type) {
        case "loadGroup": loadGroup(m); return;
        case "play": if (player) player.play(); break;
        case "pause": if (player) player.pause(); break;
        case "setSpeed": if (player) player.speed = m.value; break;
        case "setMix":
          if (player && player.animationState && player.animationState.data)
            player.animationState.data.defaultMix = m.value;
          break;
        case "setSkin":
          if (player && player.skeleton) {
            player.skeleton.setSkinByName(m.name);
            player.skeleton.setSlotsToSetupPose();
            currentSkin = m.name;
            needPose = true;
          }
          break;
        case "setAnimation":
          if (player) {
            var e = player.setAnimation(m.name, m.loop !== false);
            currentAnimation = m.name;
            loop = m.loop !== false;
            needPose = true;
            post({
              type: "trackChanged",
              animation: m.name,
              duration: e && e.animation ? e.animation.duration : 0
            });
            return;
          }
          break;
        case "setLoop":
          loop = !!m.value;
          if (player && currentAnimation) {
            var e2 = player.setAnimation(currentAnimation, loop);
            needPose = true;
            post({
              type: "trackChanged",
              animation: currentAnimation,
              duration: e2 && e2.animation ? e2.animation.duration : 0
            });
            return;
          }
          break;
        case "seek":
          var entry = currentEntry();
          if (entry) { entry.trackTime = Math.max(0, m.time); needPose = true; }
          break;
        case "setupPose":
          if (player && player.skeleton) { player.skeleton.setToSetupPose(); needPose = true; }
          break;
        case "setDebug":
          if (player && player.config && player.config.debug)
            player.config.debug.bones = !!m.value;
          break;
        case "setBackground":
          // AUD-043 ⑤：runtime 只在资产加载阶段把 config.backgroundColor 解析进
          // this.bg / this.bgFullscreen；直接改这两个 Color 对象即可实时生效，
          // 无需重建 player，播放 / 皮肤 / 动画 / 用户视图全部保持。
          bgHex = m.hex || "1E1E20";
          if (player) {
            try {
              if (player.bg && player.bg.setFromString) player.bg.setFromString(bgHex);
              if (player.bgFullscreen && player.bgFullscreen.setFromString)
                player.bgFullscreen.setFromString(bgHex);
              if (player.config) {
                player.config.backgroundColor = bgHex;
                player.config.fullScreenBackgroundColor = bgHex;
              }
            } catch (e) { }
          }
          break;
        case "resetView":
          view = { zoom: 1, panX: 0, panY: 0 };
          applyView();
          break;
        case "dispose":
          if (bootstrapTimer) { clearInterval(bootstrapTimer); bootstrapTimer = null; }
          if (player) { try { player.dispose(); } catch (e) { } player = null; disposedCount++; }
          break;
        case "probeState": {
          var cur = currentEntry();
          var info = glInfo();
          var cvp = player ? player.currentViewport : null;
          var cam = null;
          try {
            if (player && player.sceneRenderer && player.sceneRenderer.camera) {
              var cc = player.sceneRenderer.camera;
              cam = { x: cc.position.x, y: cc.position.y, zoom: cc.zoom };
            }
          } catch (e) { }
          post({
            type: "state",
            canvasCount: info.canvasCount,
            webglLost: info.webglLost,
            disposedCount: info.disposedCount,
            trackTime: cur ? cur.trackTime : -1,
            paused: player ? !!player.paused : true,
            animation: currentAnimation,
            skin: currentSkin,
            background: bgHex,
            bleed: lastBleed,
            view: { zoom: view.zoom, panX: view.panX, panY: view.panY },
            viewportLocked: viewportLocked,
            baseViewport: baseViewport,
            currentViewport: cvp
              ? { x: cvp.x, y: cvp.y, width: cvp.width, height: cvp.height }
              : null,
            camera: cam
          });
          return;
        }
        default: break;
      }
      post({ type: "ack", what: m.type });
    } catch (e) {
      post({ type: "error", phase: m.type, reason: String(e && e.message || e) });
    }
  }

  if (window.chrome && window.chrome.webview)
    window.chrome.webview.addEventListener("message", function (ev) { handle(ev.data || {}); });

  // 只读诊断接口（与 __eapPlayer / __eapCfg 同属 V3.1 有意保留的本地排查能力）：
  // 供 harness 断言视图锁定、缩放平移、背景档位与 Bleed 记录，不写回任何文件。
  try {
    window.__eapViewState = function () {
      return {
        view: { zoom: view.zoom, panX: view.panX, panY: view.panY },
        baseViewport: baseViewport,
        viewportLocked: viewportLocked,
        background: bgHex,
        bleed: lastBleed
      };
    };
  } catch (e) { }

  post({ type: "bridgeReady", spinePlayerLoaded: !!window.spine });
})();
