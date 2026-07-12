/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  eslint: {
    ignoreDuringBuilds: true,
  },
  experimental: {
    outputFileTracingRoot: __dirname,
  },
  images: {
    remotePatterns: [
      {
        protocol: "http",
        hostname: "stvcc.ru",
        pathname: "/**",
      },
    ],
  },
}

module.exports = nextConfig
