FROM mono:6

ARG VS_VERSION=1.14.5

RUN mkdir /game
RUN mkdir /code
WORKDIR /game

RUN curl https://cdn.vintagestory.at/gamefiles/stable/vs_server_${VS_VERSION}.tar.gz -o /tmp/server.tar.gz && tar xvf /tmp/server.tar.gz && rm /tmp/server.tar.gz

COPY . /code
WORKDIR /code 

RUN cp /game/*.dll /code/lib
RUN cp /game/Mods/* /code/lib
RUN cp /game/Lib/* /code/lib

RUN msbuild resinvessel.csproj -property:Configuration=Release

