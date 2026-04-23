import { ConfigBase, CreateOutput, UpdateOutput } from './api.client';

declare module './api.client' {
    interface ConfigBase {
        init(_data?: any): void;
    }
    
    interface CreateOutput {
        toJSON(data?: any): any;
    }
    
    interface UpdateOutput {
        toJSON(data?: any): any;
    }
}

ConfigBase.prototype.init = function(_data?: any) {
    if (_data) {
        for (const property in _data) {
            (this as any)[property] = (_data as any)[property];
        }
    }
};

CreateOutput.prototype.toJSON = function(data?: any) {
    data = typeof data === 'object' ? data : {};
    data["name"] = this.name;
    data["description"] = this.description;
    data["type"] = this.type;
    data["tagIds"] = this.tagIds;
    data["errorHandlingStrategy"] = this.errorHandlingStrategy;
    
    if (this.config) {
        data["config"] = {};
        for (const property in this.config) {
            if (this.config.hasOwnProperty(property) && property !== 'constructor') {
                data["config"][property] = (this.config as any)[property];
            }
        }
    } else {
        data["config"] = undefined;
    }
    
    return data;
};

UpdateOutput.prototype.toJSON = function(data?: any) {
    data = typeof data === 'object' ? data : {};
    data["name"] = this.name;
    data["description"] = this.description;
    data["type"] = this.type;
    data["tagIds"] = this.tagIds;
    data["errorHandlingStrategy"] = this.errorHandlingStrategy;
    
    if (this.config) {
        data["config"] = {};
        for (const property in this.config) {
            if (this.config.hasOwnProperty(property) && property !== 'constructor') {
                data["config"][property] = (this.config as any)[property];
            }
        }
    } else {
        data["config"] = undefined;
    }
    
    return data;
};

export { ConfigBase, CreateOutput, UpdateOutput };