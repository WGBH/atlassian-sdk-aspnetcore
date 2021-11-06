// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System;
using System.Diagnostics.CodeAnalysis;
using Atlassian.Jira.OAuth;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using JiraRoot = Atlassian.Jira.Jira;

namespace Atlassian.Jira.AspNetCore
{
    static class ValidationHelper
    {
        public static void EnsureNonNull<T>(string paramName, [NotNull] T? value) where T : class
        {
            if (value == null)
                throw new InvalidOperationException(paramName + " must not be null!");
        }
    }

    public class JiraWithBasicAuthOptions
    {
        public Uri? BaseUri { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public JiraRestClientSettings RestClientSettings { get; } = new();

        public JiraWithBasicAuthOptions()
        {
            RestClientSettings.JsonSerializerSettings.Formatting = Formatting.None;
        }

        internal void Validate()
        {
            ValidationHelper.EnsureNonNull(nameof(BaseUri), BaseUri);
        }
    }

    public class JiraWithOAuthOptions
    {
        public Uri? BaseUri { get; set; }
        public string? ConsumerKey { get; set; }
        public string? ConsumerSecret { get; set; }
        public string? OAuthAccessToken { get; set; }
        public string? OAuthTokenSecret { get; set; }
        public JiraOAuthSignatureMethod OAuthSignatureMethod { get; set; } =
            JiraOAuthSignatureMethod.RsaSha1;
        public JiraRestClientSettings RestClientSettings { get; } = new();

        public JiraWithOAuthOptions()
        {
            RestClientSettings.JsonSerializerSettings.Formatting = Formatting.None;
        }

        internal void Validate()
        {
            ValidationHelper.EnsureNonNull(nameof(BaseUri), BaseUri);
            ValidationHelper.EnsureNonNull(nameof(ConsumerKey), ConsumerKey);
            ValidationHelper.EnsureNonNull(nameof(ConsumerSecret), ConsumerSecret);
            ValidationHelper.EnsureNonNull(nameof(OAuthAccessToken), OAuthAccessToken);
            ValidationHelper.EnsureNonNull(nameof(OAuthTokenSecret), OAuthTokenSecret);
        }
    }

    public static class JiraServiceCollectionExtensions
    {
        // A little helper class to hide the root Jira object. Since this nested class is private,
        // we can't inject it into other services. This encourages injecting the actual Jira services.
        class ServiceProvider
        {
            public JiraRoot JiraRoot { get; }

            public ServiceProvider(JiraRoot jiraRoot) =>
                JiraRoot = jiraRoot;
        }

        public static IServiceCollection AddJiraWithBasicAuth(
            this IServiceCollection services, string baseUri, string? username = null, string? password = null)
        {
            var options = new JiraWithBasicAuthOptions()
            {
                BaseUri = new Uri(baseUri),
                Username = username,
                Password = password
            };

            return AddJiraWithBasicAuth(services, options);
        }

        public static IServiceCollection AddJiraWithBasicAuth(
            this IServiceCollection services, Action<JiraWithBasicAuthOptions> configureOptions)
        {
            var options = new JiraWithBasicAuthOptions();
            configureOptions(options);

            return AddJiraWithBasicAuth(services, options);
        }

        public static IServiceCollection AddJiraWithBasicAuth(
            this IServiceCollection services, JiraWithBasicAuthOptions options)
        {
            options.Validate();

            services.AddScoped(p =>
                new ServiceProvider(JiraRoot.CreateRestClient
                (
                    url: options.BaseUri!.ToString(),
                    username: options.Username,
                    password: options.Password,
                    settings: options.RestClientSettings
                )));

            AddJiraServices(services);

            return services;
        }

        public static IServiceCollection AddJiraWithOAuth(
            this IServiceCollection services, Action<JiraWithOAuthOptions> configureOptions)
        {
            var options = new JiraWithOAuthOptions();
            configureOptions(options);

            return AddJiraWithOAuth(services, options);
        }

        public static IServiceCollection AddJiraWithOAuth(
            this IServiceCollection services, JiraWithOAuthOptions options)
        {
            options.Validate();

            services.AddScoped(p =>
                new ServiceProvider(JiraRoot.CreateOAuthRestClient
                (
                    url: options.BaseUri!.ToString(),
                    consumerKey: options.ConsumerKey,
                    consumerSecret: options.ConsumerSecret,
                    oAuthAccessToken: options.OAuthAccessToken,
                    oAuthTokenSecret : options.OAuthTokenSecret,
                    oAuthSignatureMethod: options.OAuthSignatureMethod,
                    settings: options.RestClientSettings
                )));

            AddJiraServices(services);

            return services;
        }

        static void AddJiraServices(IServiceCollection services)
        {
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Versions);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Components);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Priorities);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Resolutions);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Statuses);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Links);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.RemoteLinks);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.IssueTypes);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Fields);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Issues);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Users);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Groups);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Projects);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Screens);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.ServerInfo);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.Filters);
            services.AddScoped(p => p.GetRequiredService<ServiceProvider>().JiraRoot.RestClient);
        }
    }
}