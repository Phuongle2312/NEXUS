package com.nexus;

import org.apache.catalina.WebResourceRoot;
import org.apache.catalina.core.StandardContext;
import org.apache.catalina.startup.Tomcat;
import org.apache.catalina.webresources.DirResourceSet;
import org.apache.catalina.webresources.StandardRoot;

import java.io.File;

/**
 * Chạy ứng dụng bằng Tomcat 10.1 nhúng, không cần cài Tomcat: http://localhost:8081/nexus
 * Đọc thẳng src/main/webapp và target/classes, nên phải biên dịch trước (mvn test-compile).
 */
public final class DevServer {
    private DevServer() {
    }

    public static void main(String[] args) throws Exception {
        int port = args.length > 0 ? Integer.parseInt(args[0]) : 8081;
        Tomcat tomcat = new Tomcat();
        tomcat.setBaseDir(new File("target/tomcat").getAbsolutePath());
        tomcat.setPort(port);
        tomcat.getConnector();

        StandardContext context = (StandardContext) tomcat.addWebapp("/nexus", new File("src/main/webapp").getAbsolutePath());
        WebResourceRoot resources = new StandardRoot(context);
        resources.addPreResources(new DirResourceSet(resources, "/WEB-INF/classes", new File("target/classes").getAbsolutePath(), "/"));
        context.setResources(resources);

        tomcat.start();
        System.out.println("NEXUS: http://localhost:" + port + "/nexus");
        tomcat.getServer().await();
    }
}
