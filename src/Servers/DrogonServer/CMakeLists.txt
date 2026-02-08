cmake_minimum_required(VERSION 3.16)
project(drogon-server)

set(CMAKE_CXX_STANDARD 17)

find_package(Drogon REQUIRED)

add_executable(drogon-server main.cc)
target_link_libraries(drogon-server PRIVATE Drogon::Drogon)
