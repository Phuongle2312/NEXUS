package com.nexus.servlet;

import com.nexus.util.DbUtil;
import jakarta.servlet.ServletContextEvent;
import jakarta.servlet.ServletContextListener;
import jakarta.servlet.annotation.WebListener;

/** Đóng connection pool khi ứng dụng dừng hoặc redeploy. */
@WebListener
public class AppLifecycleListener implements ServletContextListener {
    @Override
    public void contextDestroyed(ServletContextEvent event) {
        DbUtil.shutdown();
    }
}
